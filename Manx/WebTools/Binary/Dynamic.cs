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
    public delegate T ReadDelegate<T>(byte[] buffer, ref int offset);
    public delegate void WriteDelegate<T>(byte[] buffer, T value, ref int offset);
    public class DynamicConverter<T> : IBinaryConverter<T>
    {
        protected Func<T, int> _GetSize;
        protected ReadDelegate<T> _Read;
        protected WriteDelegate<T> _Write;
        public override void Init(BinaryConverter Root)
        {
          
        }
        public override int GetSize(T value)
        {
            return _GetSize(value);
        }
        public override T Read(byte[] buffer, ref int offset)
        {
            return _Read(buffer, ref offset);
        }
        public override void Write(byte[] buffer, T value, ref int offset)
        {
            _Write(buffer, value, ref offset);
        }

        public override int GetSize(object value)
        {
            return _GetSize((T)value);
        }

        //public override object ReadObj(byte[] buffer, ref int offset)
        //{
        //    return _Read(buffer, ref offset);
        //}

        public override void Write(byte[] buffer, object value, ref int offset)
        {
            _Write(buffer, (T)value, ref offset);
        }
    }
}
