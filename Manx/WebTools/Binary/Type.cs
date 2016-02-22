using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public class IgnoreAttribute : Attribute { }
    public class BinaryMethodInfo
    {
        public Type[] Arguments;
        public ReadDelegate<object> Delegate;
    }
    internal class BinaryTypeInfo
    {
        public BinaryTypeInfo(){}
        public BinaryTypeInfo(BinaryConverter Root, Type type)
        {
            Name = type.Name;
            IsSealed = type.IsSealed;
            var mehflag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;


            Properties = type.GetProperties(mehflag)
                             .Where(q => !q.IsSpecialName && null == q.GetCustomAttribute<IgnoreAttribute>())
                             .ToDictionary(q => q.Name, q => q.PropertyType.IsAbstract ? typeof(object) : q.PropertyType);

            foreach (FieldInfo f in type.GetFields(mehflag).Where(q => !q.IsSpecialName && null == q.GetCustomAttribute<IgnoreAttribute>()))
                Properties.Add(f.Name, f.FieldType.IsAbstract ? typeof(object) : f.FieldType);

            Methods = type.GetMethods(mehflag)
                                  .Where(q =>/* !q.IsSpecialName &&*/ null == q.GetCustomAttribute<IgnoreAttribute>())
                                  .ToDictionary(q => q.Name, q => BuildMethod(q, Root));

            //Events = type.GetEvents(mehflag)
            //             .Where(q => !q.IsSpecialName && null == q.GetCustomAttribute<IgnoreAttribute>())
            //             .ToDictionary(q => q.Name.StartsWith("on",true,null), q => typeof(int));// q.EventHandlerType.GetGenericArguments());
        }
        private BinaryMethodInfo BuildMethod(MethodInfo method, BinaryConverter Root)
        {
            var args = new[] { method.DeclaringType}.Concat(method.GetParameters().Select(q => q.ParameterType)).ToArray();
            var buffer = Expression.Parameter(typeof(byte[]));
            var offset = Expression.Parameter(typeof(int).MakeByRefType());
            var data = args.Select(q=>
            {
                var cnv=Root.GetConverter(q);
                var reader= cnv.GetType().GetMethods().First(w => w.Name == "Read");
                if (reader.IsGenericMethod)
                    reader = reader.MakeGenericMethod(q);
                return Expression.Call(Expression.Constant(cnv),reader,buffer,offset);
            });
            Expression body = Expression.Call(data.First(), method, data.Skip(1));
            if (body.Type == typeof(void))
                body = Expression.Block(body, Expression.Constant(null));
            var expr= Expression.Lambda<ReadDelegate<object>>(body, buffer,offset);
            return new BinaryMethodInfo() { Delegate = expr.Compile(), Arguments = args };
        }
        public string Name;
        public bool IsSealed;
        public Dictionary<string, BinaryMethodInfo> Methods;
        public Dictionary<string, Type> Properties;
    }
    public enum TypeClass
    {
        Object=0,
        List=1,
        Dictionary=2,
        Function=3,
        Value=-1
    }
    public class TypeConverter:IBinaryConverter<Type>
    {
        //private static ModuleBuilder DynamicModule;
        //private static Dictionary<int, Type> Types=new Dictionary<int, Type>();
        //static TypeConverter()
        //{
        //    AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicClasses"), AssemblyBuilderAccess.Run);
        //    DynamicModule = assembly.DefineDynamicModule("Module");
        //}
        //static int GetHashCode(IEnumerable<Type> Properties)
        //{
        //    int hash = Properties.Count();
        //    foreach (var t in Properties)
        //       hash = hash*17 + t.GetHashCode();
        //    return hash;
        //}
        //public static Type BuildType(IEnumerable<Type> Properties)
        //{
        //    var key = GetHashCode(Properties);
        //    Type type;
        //    if (Types.TryGetValue(key, out type)) return type;
        //    TypeBuilder tb = DynamicModule.DefineType("D" + (Types.Count + 1), TypeAttributes.Public);
        //    ushort q = 0;
        //    var Fields= Properties.Select(t=> tb.DefineField("F"+q++,t,FieldAttributes.Public)).ToList();

        //    var constructor= tb.DefineConstructor(MethodAttributes.Public,CallingConventions.Standard,Properties.ToArray());
        //    var constructorIL = constructor.GetILGenerator();
        //    for (q = 0; q < Fields.Count; q++)
        //    {
        //        constructorIL.Emit(OpCodes.Ldarg_S,q);
        //        constructorIL.Emit(OpCodes.Stfld, Fields[q]);
        //    }
        //    constructorIL.Emit(OpCodes.Ret);

        //    constructor =  tb.DefineConstructor(MethodAttributes.Public,CallingConventions.Standard,new Type[0]);
        //    constructorIL = constructor.GetILGenerator();
        //    constructorIL.Emit(OpCodes.Ret);

        //    type = tb.CreateType();
        //    Types.Add(key, type);
        //    return type;
        //}


        private IBinaryConverter<BinaryTypeInfo> Converter;
        private IBinaryConverter<Type[]> TypesConverter;
        private BinaryConverter Root;
        public TypeClass GetClass(Type type)
        {
            if (typeof(IList).IsAssignableFrom(type))
                return TypeClass.List;
            if (typeof(IDictionary).IsAssignableFrom(type))
                return TypeClass.Dictionary;
            if (typeof(Delegate).IsAssignableFrom(type))
                return TypeClass.Function;
            if (type.IsValueType || typeof(string) == type)
                return TypeClass.Value;
            return TypeClass.Object;
        }
        public override int GetSize(Type type)
        {
            switch (GetClass(type))
            {
                case TypeClass.List: 
                    return Root.Byte.Size + Root.GetSize(GetListType(type));
                case TypeClass.Dictionary: 
                    return Root.Byte.Size + Root.GetSize(GetDictType(type));
                case TypeClass.Function:
                    return Root.Byte.Size + TypesConverter.GetSize(GetFuncType(type));
                default:
                    return Root.Byte.Size + Converter.GetSize(new BinaryTypeInfo(Root,type));
            }
        }
        public override Type Read(byte[] buffer, ref int offset)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, Type type, ref int offset)
        {
            var cls=GetClass(type);
            Root.Byte.Write(buffer,(byte)cls,ref offset);
            switch (cls)
            {
                case TypeClass.List: 
                    Root.Write(buffer, GetListType(type), ref offset); 
                    break;
                case TypeClass.Dictionary:
                    Root.Write(buffer, GetDictType(type), ref offset);
                    break;
                case TypeClass.Function:
                    TypesConverter.Write(buffer, GetFuncType(type), ref offset);
                    break;
                default:
                    Converter.Write(buffer, new BinaryTypeInfo(Root, type), ref offset);
                    break;
            }
        }
    
        public override void Init(BinaryConverter Root)
        {
            this.Root = Root;
            TypesConverter = Root.GetTypeConverter<Type[]>();
            Converter = Root.GetTypeConverter<BinaryTypeInfo>();
        }    
        public static Type GetListType(Type type)
        {
            return type.GetElementType() ?? type.GetGenericArguments()[0];
        }
        public static Type GetDictType(Type type)
        {
            return type.GetGenericArguments()[1];
        }
        public static Type[] GetFuncType(Type type)
        {
            var invoke = type.GetMethod("Invoke");
            if (invoke==null) return null;
            return invoke.GetParameters().Select(q=>q.ParameterType).ToArray();
        }
    
        
    }
}
