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
        public IBinaryConverter<T> GetConverter<T>(BinaryConverter Root)
        {
            var type = typeof(T);
            if (!typeof(IList).IsAssignableFrom(type)) return null; 
            var el = type.GetGenericArguments()[0];
            var AT = typeof(ArrayConverter<,>).MakeGenericType(type,el);
            return Activator.CreateInstance(AT, new object[] { Root }) as IBinaryConverter<T>;
        }
    }
    public class ArrayConverter<T,TE>:IBinaryConverter<T> where T : IList<TE>
    {
        public IBinaryConverter<TE> Converter { get; set; }
        public ArrayConverter(BinaryConverter root) {
            Converter = root.GetConverter<TE>();
        }
        public int GetSize(T value)
        {
            int Size = 1;
            foreach(var E in value)
                Size += Converter.GetSize(E);
            return Size;
        }
        public void Write(byte[] buffer, T value, ref int offset)
        {
            buffer[offset++] = (byte)value.Count;
            foreach (var E in value)
                Converter.Write(buffer, E, ref offset);
        }

        public T Read(byte[] buffer, ref int offset)
        {
            var O = (T) Activator.CreateInstance(typeof(T));
            var len = buffer[offset++];
            while (len-- > 0)
                O.Add(Converter.Read(buffer, ref offset));
            return (T)O;
        }
    }
}
