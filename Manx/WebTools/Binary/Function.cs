using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public class FunctionProvider : IConverterProvider
    {
        public IBinaryConverter GetConverter(Type type, BinaryConverter Root)
        {
            if (!typeof(Delegate).IsAssignableFrom(type)) return null;
            type = typeof(FunctionConverter<>).MakeGenericType(type);
            return Activator.CreateInstance(type) as IBinaryConverter;
        }
    }
    public class FunctionConverter<T> : IBinaryConverter<T>
    {
        public override int GetSize(T value)
        {
            return 0;
        }

        public override T Read(byte[] buffer, ref int offset)
        {
            return default(T);
        }

        public override void Write(byte[] buffer, T value, ref int offset)
        {
        }
        public override void Init(BinaryConverter Root)
        {
        }
    }
}
