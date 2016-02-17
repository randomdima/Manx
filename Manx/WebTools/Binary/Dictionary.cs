using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public class DictionaryProvider : IConverterProvider
    {
        public IBinaryConverter GetConverter(Type type,BinaryConverter Root)
        {
            if (!typeof(IDictionary).IsAssignableFrom(type)) return null; 
            var el = type.GetElementType()??type.GetGenericArguments()[1];
            var AT = typeof(DictionaryConverter<,>).MakeGenericType(type, el);
            return Activator.CreateInstance(AT) as IBinaryConverter;
        }
    }
    public class DictionaryConverter<T,TE>:IBinaryConverter<T> where T : IDictionary<string,TE>
    {
        public BinaryConverter Root { get; set; }
        public IBinaryConverter Converter { get; set; }
        public override void Init(BinaryConverter root)
        {
            Root = root;
            Converter = root.GetConverter(typeof(TE));
        }
        public override int GetSize(T value)
        {
            if (value == null) return Root.UInt16.Size;
            int Size = Root.UInt16.Size;
            foreach(var E in value)
                Size += Root.String.GetSize(E.Key) + Converter.GetSize(E.Value);
            return Size;
        }
        public override void Write(byte[] buffer, T value, ref int offset)
        {
            if (value == null)
            {
                Root.UInt16.Write(buffer, (UInt16)0, ref offset);
                return;
            }
            Root.UInt16.Write(buffer,(UInt16)value.Count,ref offset);
            foreach (var E in value)
            {
                Root.String.Write(buffer, E.Key, ref offset);
                Converter.Write(buffer, E.Value, ref offset);
            }
        }
        public override T Read(byte[] buffer, ref int offset)
        {
            var len = Root.UInt16.Read(buffer,ref offset);
            var O = (T)Activator.CreateInstance(typeof(T));
            for (var q = 0; q < len; q++)
                O.Add(Root.String.Read(buffer, ref offset),
                      (TE)Converter.Read(buffer,ref offset));
            
            return O;
        }
    }
}
