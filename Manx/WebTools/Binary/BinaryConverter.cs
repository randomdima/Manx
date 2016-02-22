using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WebTools.Binary.Basic;

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
        void Init(BinaryConverter Root);
    }
    public abstract class IBinaryConverter<T> : IBinaryConverter
    {
        public abstract int GetSize(T value);
        public abstract T Read(byte[] buffer, ref int offset);
        public abstract void Write(byte[] buffer, T value, ref int offset);


        public virtual int GetSize(object value)
        {
            return GetSize((T)value);
        }

        object IBinaryConverter.Read(byte[] buffer, ref int offset)
        {
            return Read(buffer, ref offset);
        }

        public virtual void Write(byte[] buffer, object value, ref int offset)
        {
            Write(buffer, (T)value, ref offset);
        }

        public virtual void Init(BinaryConverter Root) { }
    }

    public interface IBinaryAction
    {
        void Process();             
    }
    public class BinaryConverter : IBinaryConverter
    {
        private static List<Type> DefaultTypes = new List<Type>() { 
            typeof(Type),
            typeof(Boolean),
            typeof(Byte),typeof(UInt16),typeof(Int32),
            typeof(String),           
            typeof(Object),typeof(void),
            typeof(Dictionary<string,Type>),
            typeof(Type[]),
            typeof(BinaryMethodInfo),
            typeof(Dictionary<string, BinaryMethodInfo>),            
            typeof(TypeInfo),
            typeof(Type).GetType(),
            typeof(FunctionCall)
        };
        private Dictionary<Type, IBinaryConverter> Converters;
        public readonly List<IConverterProvider> Providers;
        private Dictionary<int, UInt16> ReferenceMap;
        private Dictionary<int, UInt16> VReferenceMap;
        private object[] ReferenceStorage;
        private UInt16 ReferenceStorageLength = 0;


        public NumberConverter<Int32> Int32;
        public NumberConverter<UInt16> UInt16;
        public ByteConverter Byte;
        public BooleanConverter Bool;
        public TypeConverter Type;
        public IBinaryConverter<String> String;
        public event Action<Delegate,object[]> FunctionCall;

        public BinaryConverter()
        {
            Converters = new Dictionary<Type, IBinaryConverter>();
            ReferenceMap = new Dictionary<int, UInt16>();
            ReferenceStorage = new object[System.UInt16.MaxValue];
            AddReference(null); 
            foreach (var t in DefaultTypes)
                AddReference(t);
            Providers = new List<IConverterProvider>() {
                new ObjectProvider(),
                new ArrayProvider(),
                new DictionaryProvider(),
                new FunctionProvider()
            };
           
            InitNumbers();
            AddConverter(String = new StringConverter());
            AddConverter(Type = new TypeConverter());
            Converters.Add(typeof(Type).GetType(), Type);
            AddConverter(new FunctionCallConverter());
           
        }
        private void InitNumbers()
        {
            AddConverter(Bool=new BooleanConverter());
            AddConverter(Byte = new ByteConverter());
            AddConverter(UInt16 = new NumberConverter<UInt16>());
            AddConverter(Int32 = new NumberConverter<Int32>());
            AddConverter(new NumberConverter<Int64>());
            AddConverter(new NumberConverter<Single>());
            AddConverter(new NumberConverter<Double>());
        }

        protected void AddReference(object data,ushort key)
        {
            if (ReferenceStorageLength <= key) ReferenceStorageLength = (ushort)(key + 1);
            ReferenceStorage[key] = data;
            ReferenceMap.Add(RuntimeHelpers.GetHashCode(data), key);
        }
        public int AddReference(object data)
        {
            var key = RuntimeHelpers.GetHashCode(data);
            UInt16 map;
            if (ReferenceMap.TryGetValue(key, out map))
                return map;
            map = ReferenceStorageLength++;
            ReferenceStorage[map] = data;
            ReferenceMap.Add(key, map);
            return map;
        }
        public void AddConverter<T>(IBinaryConverter<T> converter)
        {
            Converters.Add(typeof(T), converter);
            converter.Init(this);
        }
        public IBinaryConverter<T> GetTypeConverter<T>()
        {
            return GetTypeConverter(typeof(T)) as IBinaryConverter<T>;
        }
        public IBinaryConverter GetTypeConverter(Type T)
        {
            IBinaryConverter cnv;
            Converters.TryGetValue(T, out cnv);
            if (cnv != null) return cnv;
            for (var q = Providers.Count; q-- > 0; )
                if ((cnv = Providers[q].GetConverter(T, this)) != null)
                {
                    Converters.Add(T, cnv);
                    cnv.Init(this);
                    return cnv;
                }
            return null;
        }
        private IBinaryConverter GetObjectConverter(object value)
        {
            return GetTypeConverter(value == null ? (typeof(object)) : value.GetType());
        }
        public IBinaryConverter GetConverter(Type T)
        {
            switch (Type.GetClass(T))
            {
                case TypeClass.Object:
                case TypeClass.Function:return this;
                default:return GetTypeConverter(T);
            }
        }
        public int CheckSize(object value)
        {
            VReferenceMap = ReferenceMap.ToDictionary(q => q.Key, q => q.Value);
            var x= GetSize(value);
            ReferenceMap = VReferenceMap;
            return x;
        }
        public byte[] Convert(object value)
        {
            var res = new byte[CheckSize(value)];
            var offset = 0;
            Write(res, value, ref offset);
            return res;
        }
        public T Convert<T>(byte[] data)
        {
            var offset = 0;
            return Read<T>(data, ref offset);
        }
        public void Process(byte[] data)
        {
            var x = Convert<IBinaryAction>(data);
            if (x != null) x.Process();
        }
        public object Convert(byte[] data)
        {
            var offset = 0;
            return Read(data, ref offset);
        }

        public int GetSize(object value)
        {
            var key = RuntimeHelpers.GetHashCode(value);
            if (ReferenceMap.ContainsKey(key))
                return UInt16.Size;            
            ReferenceMap.Add(key, 1);
            var type = value.GetType();
            int size = UInt16.Size + GetTypeConverter(type).GetSize(value);
           // if (type.IsSealed) return size;
            return size + GetSize(type); 
        }
        public int GetSize<T>(object value)
        {
            var key = RuntimeHelpers.GetHashCode(value);
            if (ReferenceMap.ContainsKey(key))
                return UInt16.Size;
            ReferenceMap.Add(key, 1);
            var type = value.GetType();
            int size = UInt16.Size + GetTypeConverter(type).GetSize(value);
            if (typeof(T).IsSealed) return size;
            return size + GetSize(type);
        }
        public bool WriteReference(byte[] buffer, object value, ref int offset)
        {
            var key = RuntimeHelpers.GetHashCode(value);
            UInt16 map;
            var exists= ReferenceMap.TryGetValue(key, out map);
            if (!exists)
            {
                map = ReferenceStorageLength++;
                ReferenceStorage[map] = value;
                ReferenceMap.Add(key, map);
            }
            UInt16.Write(buffer, map, ref offset);
            return exists;
        }
       
        public void Write<T>(byte[] buffer, T value, ref int offset)
        {
            if (WriteReference(buffer, value, ref offset)) return;
            if(!typeof(T).IsSealed)
                Write(buffer, value.GetType(), ref offset);
            GetObjectConverter(value).Write(buffer, value, ref offset);
        }

        public void Write(byte[] buffer, object value, ref int offset)
        {
            if (WriteReference(buffer, value, ref offset)) return;
            Write(buffer, value.GetType(), ref offset);
            GetObjectConverter(value).Write(buffer, value, ref offset);
        }
        
        public T Read<T>(byte[] buffer, ref int offset)
        {
            var key = UInt16.Read(buffer, ref offset);
            if (ReferenceStorageLength > key) return (T)ReferenceStorage[key];

            var type = typeof(T);
            if(!type.IsSealed) type=Read<Type>(buffer, ref offset);
            var value = (T)GetTypeConverter(type).Read(buffer, ref offset);
            AddReference(value, key);
            return value;
        }
        public object Read(byte[] buffer, ref int offset)
        {
            var key = UInt16.Read(buffer, ref offset);
            if (ReferenceStorageLength > key) return ReferenceStorage[key];

            var type = Read<Type>(buffer,ref offset);
            var value = GetTypeConverter(type).Read(buffer,ref offset);
            AddReference(value,key);
            return value;
        }

        public void Init(BinaryConverter Root)
        {
            throw new NotImplementedException();
        }

        public void onFunctionCall(Delegate fn,object[] args)
        {
            if(FunctionCall!=null) FunctionCall(fn, args);
        }
    }
}
