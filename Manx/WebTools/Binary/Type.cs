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
    internal class BinaryTypeInfo
    {
        public BinaryTypeInfo() { }
        public BinaryTypeInfo(Type type)
        {
            Name = type.Name;
            IsSealed = type.IsSealed;
            var mehflag=BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            Methods = type.GetMethods(mehflag).Where(q => !q.IsSpecialName).ToDictionary(q => q.Name, q => q.ReturnType);
            Properties = type.GetProperties(mehflag).Where(q => !q.IsSpecialName).ToDictionary(q => q.Name, q => q.PropertyType);
            foreach (FieldInfo f in type.GetFields(mehflag).Where(q => !q.IsSpecialName))
                Properties.Add(f.Name, f.FieldType);
            Events = type.GetEvents(mehflag).Where(q => !q.IsSpecialName).ToDictionary(q => q.Name, q => typeof(int));// q.EventHandlerType.GetGenericArguments());
        }
        public string Name { get; set; }
        public bool IsSealed { get; set; }
        public Dictionary<string, Type> Methods { get; set; }
        public Dictionary<string, Type> Properties { get; set; }
        public Dictionary<string, Type> Events { get; set; }
    }
    public enum TypeClass
    {
        Object=0,
        List=1,
        Dictionary=2,
        Function=3
    }
    public class TypeConverter:IBinaryConverter<Type>
    {
        private static ModuleBuilder DynamicModule;
        private static int DynamicClassCount = 0;
        static TypeConverter()
        {
            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicClasses"), AssemblyBuilderAccess.Run);
            DynamicModule = assembly.DefineDynamicModule("Module");
        }
        public static Type BuildType(Dictionary<string, Type> Properties)
        {
            TypeBuilder tb = DynamicModule.DefineType("DynamicClass" + DynamicClassCount++, TypeAttributes.Public|TypeAttributes.Sealed);
            foreach (var f in Properties)
            {
                tb.DefineField(f.Key, f.Value,FieldAttributes.Public);
            }
            return tb.CreateType();
        }


        private IBinaryConverter<BinaryTypeInfo> Converter;
        private BinaryConverter Root;
        protected TypeClass GetClass(Type type)
        {
            if (typeof(IList).IsAssignableFrom(type))
                return TypeClass.List;
            if (typeof(IDictionary).IsAssignableFrom(type))
                return TypeClass.Dictionary;
            if (typeof(Delegate).IsAssignableFrom(type))
                return TypeClass.Function;
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
                    return Root.Byte.Size + Root.GetSize(GetFuncType(type));
                default:
                    return Root.Byte.Size + Converter.GetSize(new BinaryTypeInfo(type));
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
                    Root.Write(buffer, GetFuncType(type), ref offset);
                    break;
                default:
                    Converter.Write(buffer, new BinaryTypeInfo(type), ref offset);
                    break;
            }
        }
    
        public override void Init(BinaryConverter Root)
        {
            this.Root = Root;
            Converter = new ObjectConverter<BinaryTypeInfo>();
            Converter.Init(Root);
        }    
        public Type GetListType(Type type)
        {
            return type.GetElementType() ?? type.GetGenericArguments()[0];
        }
        public Type GetDictType(Type type)
        {
            return type.GetGenericArguments()[1];
        }
        public Type GetFuncType(Type type)
        {
            var param= type.GetMethod("Invoke").GetParameters().ToDictionary(q=>q.Name,q=>q.ParameterType);
            return BuildType(param);
        }
    
        
    }
}
