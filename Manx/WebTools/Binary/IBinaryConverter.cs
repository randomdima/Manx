using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public interface IConverterProvider
    {
        IBinaryConverter<T> GetConverter<T>(BinaryConverter Root);
    }
    public interface IBinaryConverter<T>
    {
        int GetSize(T value);
        T Read(byte[] buffer, ref int offset);
        void Write(byte[] buffer, T value, ref int offset);
    }
    public class BinaryConverter
    {
        private Dictionary<Type, object> Converters;
        public readonly List<IConverterProvider> Providers;
        public Dictionary<int, object> ReferenceStorage;
        public readonly IBinaryConverter<int> Int32;

        public BinaryConverter()
        {
            ReferenceStorage = new Dictionary<int, object>();
            Converters = new Dictionary<Type, object>();
            Providers = new List<IConverterProvider>() {
                new ObjectProvider(),
                new ArrayProvider(),
                new NumberProvider(),
                new StringProvider()
            };
            Int32 = GetConverter<int>();
        }

        public IBinaryConverter<T> GetConverter<T>()
        {
            IBinaryConverter<T> cnv;
            object x = null;
            Converters.TryGetValue(typeof(T), out x);
            cnv = x as IBinaryConverter<T>;
            if (cnv != null) return cnv;
            for (var q = Providers.Count; q-- > 0;)
                if ((cnv = Providers[q].GetConverter<T>(this)) != null)
                {
                    Converters.Add(typeof(T), cnv);
                    return cnv;
                }
            return null;
        }
        protected MethodInfo _GetConverter = typeof(BinaryConverter).GetMethod("GetConverter");
        public object GetConverterObj(Type T)
        {
            object cnv = null;
            Converters.TryGetValue(T, out cnv);
            return cnv ?? _GetConverter.MakeGenericMethod(T).Invoke(this, new object[] { });
        }

        public byte[] Convert<T>(T value)
        {
            var cnv = GetConverter<T>();
            var oldref = ReferenceStorage;
            ReferenceStorage = oldref.ToDictionary(q => q.Key, q => q.Value);
            var x = new byte[cnv.GetSize(value)];
            ReferenceStorage = oldref;
            int offset = 0;
            cnv.Write(x, value, ref offset);
            return x;
        }
        public T Convert<T>(byte[] buffer)
        {
            int offset = 0;
            return GetConverter<T>().Read(buffer, ref offset);
        }
    }
}
