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
    public interface IRefObject { }
    public class ObjectProvider : IConverterProvider
    {
        public IBinaryConverter<T> GetConverter<T>(BinaryConverter Root)
        {
            return new ObjectConverter<T>(Root);
        }
    }

    
    public delegate T ReadDelegate<T>(byte[] buffer, ref int offset);
    public delegate void WriteDelegate<T>(byte[] buffer, T value, ref int offset);
    public class ObjectConverter<T>: IBinaryConverter<T>
    {
        protected BinaryConverter Root;
        protected Func<T,int> _GetSize;
        protected ReadDelegate<T> _Read;
        protected WriteDelegate<T> _Write;
 
        public ObjectConverter(BinaryConverter Root)
        {
            this.Root = Root;
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
                var converter = Expression.Constant(Root.GetConverterObj(P.PropertyType));
                var ct = converter.Value.GetType();
                Sizer.Add(Expression.Call(converter, ct.GetMethod("GetSize"), Expression.Property(value, P)));
                Writer.Add(Expression.Call(converter, ct.GetMethod("Write"), buffer, Expression.Property(value, P),offset));
                Reader.Add(Expression.Assign(Expression.Property(ReaderObj, P), Expression.Call(converter, ct.GetMethod("Read"), buffer, offset)));
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
            _Write = Expression.Lambda<WriteDelegate<T>>(Expression.Block(Writer),buffer,value,offset).Compile();
            Reader.Add(ReaderObj);
            _Read = Expression.Lambda<ReadDelegate<T>>(Expression.Block(new ParameterExpression[] { ReaderObj },Reader),buffer,offset).Compile();
        }
        public int GetSize(T value) {
            int key = RuntimeHelpers.GetHashCode(value);
            if (Root.ReferenceStorage.ContainsKey(key)) return 4;
            Root.ReferenceStorage.Add(key, value);
            return _GetSize(value)+4;
        }
        public T Read(byte[] buffer, ref int offset) {
            var key=Root.Int32.Read(buffer, ref offset);
            if (key > 0) return (T)Root.ReferenceStorage[key];
            return _Read(buffer, ref offset);

        }
        public void Write(byte[] buffer, T value, ref int offset) {
            int key = RuntimeHelpers.GetHashCode(value);
            Root.Int32.Write(buffer, key, ref offset);
            if (!Root.ReferenceStorage.ContainsKey(key))
            {
                Root.ReferenceStorage.Add(key, value);
                _Write(buffer, value, ref offset);
            }
        }
    }
}
