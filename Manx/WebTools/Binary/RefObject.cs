using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public interface IRefObject { }
    public class RefObjectProvider : ObjectProvider
    {
        public override IBinaryConverter GetConverter(Type type, BinaryConverter Root)
        {
            if (typeof(IRefObject).IsAssignableFrom(type) || type==typeof(object))
            {
                type = typeof(RefObjectConverter<>).MakeGenericType(type);
                return Activator.CreateInstance(type, new object[] { Root }) as IBinaryConverter;
            }
            return base.GetConverter(type, Root);
        }
    }
    public class RefObjectConverter<T> : ObjectConverter<T>
    {
        public RefObjectConverter(BinaryConverter Root)
            : base(Root)
        {
        }
        public override int GetSize(T value)
        {
            if (value == null) return 4;
            int key = value.GetHashCode();
            if (Root.ReferenceStorage.ContainsKey(key)) return 4;
            Root.ReferenceStorage.Add(key, value);
            return base.GetSize(value) + 4;
        }
        public override T Read(byte[] buffer, ref int offset)
        {
            var key = Root.Int32.Read(buffer, ref offset);
            return (T)Root.ReferenceStorage[key];
        }
        public override void Write(byte[] buffer, T value, ref int offset)
        {
            if (value != null)
            {
                int key = value.GetHashCode();
                Root.Int32.Write(buffer, key, ref offset);
                if (Root.ReferenceStorage.ContainsKey(key)) return;
                Root.ReferenceStorage.Add(key, value);
            }
            base.Write(buffer, value, ref offset);
        }

    }
}
