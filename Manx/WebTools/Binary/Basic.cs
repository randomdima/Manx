﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public class NumberProvider : IConverterProvider
    {
        public IBinaryConverter<T> GetConverter<T>(BinaryConverter Root)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return new NumberConverter<T>();
                default:
                    return null;
            }
        }
    }

    public class NumberConverter<T> : IBinaryConverter<T>
    {
        public int GetSize(T value)
        {
            return 4;
        }
        public T Read(byte[] buffer, ref int offset)
        {
            var x= BitConverter.ToInt32(buffer, offset);
            offset += 4;
            return (T)(object)x;
        }        
        public void Write(byte[] buffer, T value, ref int offset)
        {
            var data = BitConverter.GetBytes((int)(object)value);
            Buffer.BlockCopy(data, 0, buffer, offset, 4);
            offset += 4;
        }
    }

    public class StringProvider : IConverterProvider
    {
        public IBinaryConverter<T> GetConverter<T>(BinaryConverter Root)
        {
            if (typeof(T) != typeof(string)) return null;
            return (IBinaryConverter<T>)(object)(new StringConverter());
        }
    }
    public class StringConverter : IBinaryConverter<string>
    {
        protected Encoding encoder = Encoding.UTF8;
        public int GetSize(string value)
        {
            return encoder.GetByteCount(value)+1;
        }
        public string Read(byte[] buffer, ref int offset)
        {
            var len = buffer[offset++];
            var str= encoder.GetString(buffer,offset,len);
            offset += len;
            return str;
        }
        public void Write(byte[] buffer, string value, ref int offset)
        {
            var len = encoder.GetBytes(value, 0, value.Length, buffer, offset+1);
            buffer[offset++] = (byte)len;
            offset += len;
        }
    }
}
