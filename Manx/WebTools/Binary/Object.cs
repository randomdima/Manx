using System;
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
    public class ObjectProvider : IConverterProvider
    {
        public virtual IBinaryConverter GetConverter(Type type,BinaryConverter Root)
        {
            type= typeof(ObjectConverter<>).MakeGenericType(type);
            return Activator.CreateInstance(type) as IBinaryConverter;
        }
    }
    public class ObjectConverter<T>: DynamicConverter<T>
    {
        public override void Init(BinaryConverter Root)
        {
            var type = typeof(T);
            var value = Expression.Parameter(type, "v");
            var offset = Expression.Parameter(typeof(int).MakeByRefType(), "o");
            var buffer = Expression.Parameter(typeof(byte[]), "b");
            var root = Expression.Constant(Root);

            var Sizer = new List<Expression>();
            var Writer = new List<Expression>();
            var Reader = new List<Expression>();
            var ReaderObj = Expression.Variable(type, "O");
            Reader.Add(Expression.Assign(ReaderObj, Expression.New(type)));
            var mehflag=BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            foreach (var P in type.GetProperties(mehflag).Where(q=>!q.IsSpecialName))
            {
                var converter = Expression.Constant(Root.GetConverter(P.PropertyType));
                var ct = converter.Value.GetType();
                var methods=ct.GetMethods();
                var method = methods.First(q => q.Name == "GetSize");
                Sizer.Add(Expression.Call(converter, method, Expression.Property(value, P)));

                method = methods.First(q => q.Name == "Write");
                if (method.IsGenericMethod)
                    method = method.MakeGenericMethod(P.PropertyType);
                Writer.Add(Expression.Call(converter, method, buffer, Expression.Property(value, P), offset));

                if (P.SetMethod != null)
                {
                    method = methods.First(q => q.Name == "Read");
                    if (method.IsGenericMethod)
                        method = method.MakeGenericMethod(P.PropertyType);
                    Reader.Add(Expression.Assign(Expression.Property(ReaderObj, P), Expression.Call(converter, method, buffer, offset)));
                }
            }   
            foreach (var P in type.GetFields(mehflag).Where(q=>!q.IsSpecialName))
            {
                var converter = Expression.Constant(Root.GetConverter(P.FieldType));
                var ct = converter.Value.GetType();
                var methods=ct.GetMethods();
                var method = methods.First(q => q.Name == "GetSize");
                Sizer.Add(Expression.Call(converter, method, Expression.Field(value, P)));

                method = methods.First(q => q.Name == "Write");
                if (method.IsGenericMethod)
                    method = method.MakeGenericMethod(P.FieldType);
                Writer.Add(Expression.Call(converter, method, buffer, Expression.Field(value, P), offset));

                method = methods.First(q => q.Name == "Read");
                if (method.IsGenericMethod)
                    method = method.MakeGenericMethod(P.FieldType);
                Reader.Add(Expression.Assign(Expression.Field(ReaderObj, P), Expression.Call(converter, method, buffer, offset)));                
            }
            Expression exp;
            if (Sizer.Count == 0) exp = Expression.Constant(0);
            else if (Sizer.Count == 1)
                exp = Sizer[0];
            else
            {
                exp = Expression.Add(Sizer[0], Sizer[1]);
                for (var q = 2; q < Sizer.Count; q++)
                    exp = Expression.Add(exp, Sizer[q]);
            }
            _GetSize = Expression.Lambda<Func<T, int>>(exp, value).Compile();
            if (Writer.Count == 0) Writer.Add(Expression.Constant(0));
            _Write = Expression.Lambda<WriteDelegate<T>>(Expression.Block(Writer), buffer, value, offset).Compile();
            Reader.Add(ReaderObj);
            _Read = Expression.Lambda<ReadDelegate<T>>(Expression.Block(new ParameterExpression[] { ReaderObj }, Reader), buffer, offset).Compile();
        }
    }
}
