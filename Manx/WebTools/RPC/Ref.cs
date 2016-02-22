//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json.Serialization;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using System.Runtime.Serialization;
//using System.Text;
//using System.Threading.Tasks;

//namespace WebTools.RPC
//{
//    public class JsonRefSerializer
//    {
//        public JsonSerializer JSS;
//        public JsonRefSerializer(RPCSocketClient Client)
//        {
//            var Settings = new JsonSerializerSettings();
//            //Settings.Converters.Add(new JsonTypeConverter());
//            Settings.Converters.Add(new JsonRPCMessageConverter() { Client = Client });
//            //Settings.Converters.Add(new JsonRefConverter());
//            Settings.NullValueHandling = NullValueHandling.Ignore;
//            Settings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
//            Settings.TypeNameHandling = TypeNameHandling.Objects;
//            Settings.ReferenceResolverProvider = () => new JsonRefResolver();
//            Settings.Binder = new JsonTypeBInder();
//            JSS = JsonSerializer.Create(Settings);
//        }
//        public T Deserialize<T>(string json)
//        {
//            using (var R = new StringReader(json))
//            using (var RR = new JsonTextReader(R))
//                return JSS.Deserialize<T>(RR);
//        }
//        public string Serialize(object obj)
//        {
            
//            using (var R = new StringWriter())
//            using (var RR = new JsonTextWriter(R))
//            {
//                RR.QuoteName = false;
//                JSS.Serialize(RR, obj);
//                return R.ToString();
//            }
//        }
//    }
//    public interface IRefObject
//    {
//    }
//    public class JsonTypeBInder : SerializationBinder
//    {
//        protected List<Type> Types = new List<Type>();     
//        public override Type BindToType(string assemblyName, string typeName)
//        {
//            return Types[int.Parse(typeName)];
//        }
//        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
//        {
//            assemblyName = null;
//            if (!typeof(IRefObject).IsAssignableFrom(serializedType))
//            {
//                typeName = null;
//                return;
//            }
//            int I = Types.IndexOf(serializedType);
//            if (I >= 0) typeName = I.ToString();
//            else
//            {
//                Types.Add(serializedType);
//               // typeName = "qwe";return;
//                StringBuilder sb = new StringBuilder();
//                var methods = serializedType.GetMethods().Select(q => q.Name).ToArray();
//                if (methods.Length > 0) { 
//                    sb.Append("methods:['"); sb.Append(string.Join("','", methods)); sb.Append("'],");
//                }
//                var events = serializedType.GetEvents().Select(q => q.Name).ToArray();
//                if (events.Length > 0)
//                {
//                    sb.Append("events:['"); sb.Append(string.Join("','", events)); sb.Append("'],");
//                }
//                sb.Append("id:"); sb.Append(Types.Count);
//                typeName = sb.ToString();
//            }
//        }
//    }
//    public class JsonRefResolver : IReferenceResolver
//    {
//        protected Dictionary<int, object> Storage = new Dictionary<int, object>();
//        public void AddReference(object context, string reference, object value)
//        {
//            Storage.Add(int.Parse(reference),value);
//        }

//        public string GetReference(object context, object value)
//        {
//            if (value is IRefObject)
//            {
//                var key = RuntimeHelpers.GetHashCode(value);
//                if (!Storage.ContainsKey(key))
//                    Storage.Add(key, value);
//                return key.ToString();
//            }
//            return null;
//        }

//        public bool IsReferenced(object context, object value)
//        {
//            if (value is IRefObject)
//                return Storage.ContainsKey(RuntimeHelpers.GetHashCode(value));
//            return false;
//        }

//        public object ResolveReference(object context, string reference)
//        {
//            object x;
//            Storage.TryGetValue(int.Parse(reference), out x);
//            return x;
//        }
//    }
    
//    public class JsonRPCMessageConverter : JsonConverter
//    {
//        List<Type> MessageType = new List<Type>() { typeof(RPCEventMessage),typeof(RPCBindMessage),typeof(RPCInvokeMessage) };
//        public RPCSocketClient Client { get; set; }
//        public override bool CanConvert(Type objectType)
//        {
//           return typeof(RPCMessage).IsAssignableFrom(objectType);
//        }
//        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//        {
//            JObject jObject = JObject.Load(reader);

//            var type = MessageType[jObject.Value<int>("type")];
//            var obj = Activator.CreateInstance(type) as RPCMessage;
//            obj.Client = Client;
//            // Populate the object properties
//            serializer.Populate(jObject.CreateReader(), obj);

//            return obj;
//        }

//        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//        {           
//            Type type = value.GetType();
//            writer.WriteStartObject();

//            writer.WritePropertyName("type");
//            serializer.Serialize(writer, MessageType.IndexOf(type));
//            foreach (PropertyInfo prop in type.GetProperties())
//            {
//                object propVal = prop.GetValue(value, null);
//                if (propVal != null)
//                {
//                    writer.WritePropertyName(prop.Name);
//                    // writer.WriteValue(propVal);
//                    serializer.Serialize(writer, propVal);
//                }
//            }
//            writer.WriteEndObject();
//        }
//    }
//}
