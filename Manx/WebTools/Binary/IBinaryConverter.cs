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
        IBinaryConverter GetConverter(Type type,BinaryConverter Root);
    }
    public interface IBinaryConverter
    {
        int GetSize(object value);
        object Read(byte[] buffer, ref int offset);
        void Write(byte[] buffer, object value, ref int offset);
        void Init();
    }
    public abstract class IBinaryConverter<T> : IBinaryConverter
    {
        public abstract int GetSize(T value);
        public abstract T Read(byte[] buffer, ref int offset);
        public abstract void Write(byte[] buffer, T value, ref int offset);


        public int GetSize(object value)
        {
            return GetSize((T)value);
        }

        object IBinaryConverter.Read(byte[] buffer, ref int offset)
        {
            return Read(buffer, ref offset);
        }

        public void Write(byte[] buffer, object value, ref int offset)
        {
            Write(buffer, (T)value, ref offset);
        }

        public virtual void Init() { }
    }

    public class BinaryConverter : IBinaryConverter
    {
        private static List<Type> DefaultTypes = new List<Type>() { 
            typeof(Int32),typeof(String),typeof(Type),typeof(Object),typeof(BinaryTypeInfo),typeof(BinaryMemberInfo)
        };
        private Dictionary<Type, IBinaryConverter> Converters;
        public readonly List<IConverterProvider> Providers;
        public Dictionary<int, object> ReferenceStorage;
        public readonly IBinaryConverter<int> Int32;

        public BinaryConverter(bool RefObjects=false)
        {
            ReferenceStorage = new Dictionary<int, object>() {{0,null}};
            foreach (var t in DefaultTypes) ReferenceStorage.Add(t.Name.GetHashCode(), t);
            Converters = new Dictionary<Type, IBinaryConverter>();
            var ObjPrv = RefObjects ? (IConverterProvider)new RefObjectProvider() : new ObjectProvider();
            Providers = new List<IConverterProvider>() {
                ObjPrv,
                new ArrayProvider(),
                new NumberProvider(),
                new StringProvider()
            };
            Int32 = GetConverter<int>();
        }

        public IBinaryConverter<T> GetConverter<T>()
        {
            return GetConverter(typeof(T)) as IBinaryConverter<T>; 
        }
        public IBinaryConverter GetConverter(Type T)
        {
            IBinaryConverter cnv;
            Converters.TryGetValue(T, out cnv);
            if (cnv != null) return cnv;
            for (var q = Providers.Count; q-- > 0;)
                if ((cnv = Providers[q].GetConverter(T,this)) != null)
                {
                    Converters.Add(T, cnv);
                    cnv.Init();
                    return cnv;
                }
            return null;
        }
        public IBinaryConverter GetConverter(object value,Type T=null)
        {
            if (value == null) return GetConverter(T??typeof(object));
            return GetConverter(value.GetType());
        }

        private Dictionary<int, object> oldRef;
        public void PushRef()
        {
            oldRef=ReferenceStorage.ToDictionary(q => q.Key, q => q.Value);  
        }
        public void PopRef()
        {
            ReferenceStorage = oldRef;
            oldRef = null;
        }
        public byte[] Convert<T>(T value)
        {
            var cnv = GetConverter(value, typeof(T));
            PushRef();          
            var res = new byte[cnv.GetSize(value)];
            PopRef();
            var offset = 0;
            cnv.Write(res,value,ref offset);
            return res;
        }
        public T Convert<T>(byte[] buffer)
        {
            int offset = 0;
            return (T)GetConverter(typeof(T)).Read(buffer, ref offset);
        }

        //public int GetSize<T>(T value)
        //{
        //    Type type = value == null ? typeof(T) : value.GetType();
        //    var cnv=GetConverter(type);
        //    return (int)cnv.GetType().GetMethod("GetSize").Invoke(cnv, new object[] { value });
        //}
        //public T Read<T>(byte[] buffer, ref int offset)
        //{
            
        //  return GetConverter<T>().Read(buffer,ref offset);
        //}
        //public void Write<T>(byte[] buffer, T value, ref int offset)
        //{
        //    Type type = value == null ? typeof(T) : value.GetType();
        //    var cnv = GetConverter(type);
        //    cnv.GetType().GetMethod("Write").Invoke(cnv, new object[] { buffer, value, offset });
        //}

        public int GetSize(object value)
        {
           return GetConverter(value).GetSize(value);
        }
        public T Read<T>(byte[] buffer, ref int offset)
        {
           return GetConverter<T>().Read(buffer, ref offset);
        }
        public object Read(byte[] buffer, ref int offset)
        {
            throw new Exception("qwe");
           // return null;// GetConverter(value).GetSize(value);
        }

        public void Write(byte[] buffer, object value, ref int offset)
        {
            GetConverter(value).Write(buffer,value,ref offset);
        }


        public void Init()
        {
            throw new NotImplementedException();
        }
    }
}
