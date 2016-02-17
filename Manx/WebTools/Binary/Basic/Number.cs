using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary.Basic
{
    public class ByteConverter : NumberConverter<Byte>
    {
        public override void Init(BinaryConverter Root)
        {
            Size = 1;
        }
        public override byte Read(byte[] buffer, ref int offset)
        {
            return buffer[offset++];
        }
        public override void Write(byte[] buffer, byte value, ref int offset)
        {
            buffer[offset++]=value;
        }
    }
    public class NumberConverter<T> : IBinaryConverter<T>
    {     
        protected Func<T, byte[]> writer;
        protected Func<byte[],int, T> reader;
        public int Size;
        public override void Init(BinaryConverter Root)
        {
            var type = typeof(T);
            var code = Type.GetTypeCode(type);
          
            var offset = Expression.Parameter(typeof(int), "o");
            var value = Expression.Parameter(type, "v");
            var buffer = Expression.Parameter(typeof(byte[]), "b");
            reader = Expression.Lambda<Func<byte[], int, T>>(Expression.Call(typeof(BitConverter).GetMethod("To" + code), buffer, offset), buffer, offset).Compile();
            writer = Expression.Lambda<Func<T, byte[]>>(Expression.Call(typeof(BitConverter).GetMethod("GetBytes", new Type[] { type }), value), value).Compile();
            Size = writer(default(T)).Length;
        }
        public override int GetSize(T value)
        {
            return Size;
        }
        public override int GetSize(object value)
        {
            return Size;
        }
        public override T Read(byte[] buffer, ref int offset)
        {
            var x = reader(buffer,offset);
            offset += Size;
            return x;
        }
        public override void Write(byte[] buffer, T value, ref int offset)
        {
           var data=  writer(value);
           Buffer.BlockCopy(data, 0, buffer, offset, Size);
           offset += Size;
        }
    }
}
