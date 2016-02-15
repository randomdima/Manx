using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public class ArrayProvider : IConverterProvider
    {
        public IBinaryConverter GetConverter(Type type,BinaryConverter Root)
        {
            if (!typeof(IList).IsAssignableFrom(type)) return null; 
            var el = type.GetElementType()??type.GetGenericArguments()[0];
            var AT = typeof(ListConverter<,>).MakeGenericType(type, el);
            return Activator.CreateInstance(AT, new object[] { Root }) as IBinaryConverter;
        }
    }
    public class ListConverter<T,TE>:IBinaryConverter<T> where T : IList<TE>
    {
        public BinaryConverter Converter { get; set; }
        public ListConverter(BinaryConverter root)
        {
            Converter = root;//.GetConverter<TE>();
        }
        public override int GetSize(T value)
        {
            if (value == null) return 1;
            int Size = 1;
            foreach(var E in value)
                Size += Converter.GetSize(E);
            return Size;
        }
        public override void Write(byte[] buffer, T value, ref int offset)
        {
            if (value == null)
            {
                buffer[offset++] = 0;
                return;
            }
            buffer[offset++] = (byte)value.Count;
            foreach (var E in value)
                Converter.Write(buffer, E, ref offset);
        }
        public override T Read(byte[] buffer, ref int offset)
        {
            var len = buffer[offset++];
            var O = (T) Activator.CreateInstance(typeof(T),new object[]{len});
            for (var q = 0; q < len; q++)
                O[q]=Converter.Read<TE>(buffer, ref offset);
            return (T)O;
        }
    }
}
