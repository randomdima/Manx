﻿using System;
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
            return Activator.CreateInstance(AT) as IBinaryConverter;
        }
    }
    public class ListConverter<T,TE>:IBinaryConverter<T> where T : IList<TE>
    {
        public IBinaryConverter Converter { get; set; }
        public BinaryConverter Root { get; set; }
        public override void Init(BinaryConverter root)
        {
            this.Root = root;
            Converter = root.GetConverter(typeof(TE));
        }
        public override int GetSize(T value)
        {
            if (value == null) return Root.UInt16.Size;
            int Size = Root.UInt16.Size;
            foreach(var E in value)
                Size += Converter.GetSize(E);
            return Size;
        }
        public override void Write(byte[] buffer, T value, ref int offset)
        {
            if (value == null) {
                Root.UInt16.Write(buffer, (UInt16)0, ref offset);
                return;
            }
            Root.UInt16.Write(buffer, (UInt16)value.Count, ref offset);
            foreach (var E in value)
                Converter.Write(buffer, E, ref offset);
        }
        public override T Read(byte[] buffer, ref int offset)
        {
            var len = Root.UInt16.Read(buffer, ref offset);
            var O = (T) Activator.CreateInstance(typeof(T),new object[]{len});
            for (var q = 0; q < len; q++)
                O[q] = (TE)Converter.Read(buffer, ref offset);
            return O;
        }
    }
}
