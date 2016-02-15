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
            return Activator.CreateInstance(type, new object[] { Root }) as IBinaryConverter;
        }
    }
    public delegate T ReadDelegate<T>(byte[] buffer, ref int offset);
    public delegate void WriteDelegate<T>(byte[] buffer, T value, ref int offset);
    public class ObjectConverter<T>: IBinaryConverter<T>
    {
        protected BinaryConverter Root;
        private Func<T, int> _GetSize;
        private ReadDelegate<T> _Read;
        private WriteDelegate<T> _Write; 
        public ObjectConverter(BinaryConverter Root)
        {           
            this.Root = Root;
        }
        public override void Init()
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
            foreach (var P in type.GetProperties())
            {
                var converter = P.PropertyType.IsSealed ? Expression.Constant(Root.GetConverter(P.PropertyType)) : root;
                var ct = converter.Value.GetType();
                var method = ct.GetMethods().First(q => q.Name == "GetSize");
                Sizer.Add(Expression.Call(converter, method, Expression.Property(value, P)));

                method = ct.GetMethods().First(q => q.Name == "Write");
                Writer.Add(Expression.Call(converter, method, buffer, Expression.Property(value, P), offset));

                if (P.SetMethod != null)
                {
                    method = ct.GetMethods().First(q => q.Name == "Read");
                    if (method.IsGenericMethod)
                        method = method.MakeGenericMethod(P.PropertyType);
                    Reader.Add(Expression.Assign(Expression.Property(ReaderObj, P), Expression.Call(converter, method, buffer, offset)));
                }

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
        public override int GetSize(T value)
        {
            if (value == null) return 4;
            var type = value.GetType();
            if (type.IsSealed) return _GetSize(value);
            return Root.GetSize(new BinaryTypeInfo(type)) + _GetSize(value);
        }
        public override T Read(byte[] buffer, ref int offset)
        {
            var type = Root.Int32.Read(buffer,ref offset);
            return _Read(buffer, ref offset);
        }
        public override void Write(byte[] buffer, T value, ref int offset)
        {
            if (value == null)
            {
                Root.Int32.Write(buffer, 0, ref offset);
                return;
            } 
            var type = value.GetType();
            if (!type.IsSealed) Root.Write(buffer, new BinaryTypeInfo(type), ref offset);
            _Write(buffer, value, ref offset);
        }
    }
}
