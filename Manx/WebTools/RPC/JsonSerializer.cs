using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.RPC
{
    public class JsonRefSerializer
    {
        public JsonSerializer JSS;
        public JsonRefSerializer(RPCSocketClient Client)
        {
            var Settings = new JsonSerializerSettings();
            Settings.NullValueHandling = NullValueHandling.Ignore;
            Settings.Converters.Add(new JsonTypeConverter());
            Settings.Converters.Add(new JsonRPCMessageConverter() { Client = Client });
            Settings.Converters.Add(new JsonRefConverter());
           
            JSS = JsonSerializer.Create(Settings);
        }
        public T Deserialize<T>(string json)
        {
            using (var R = new StringReader(json))
            using (var RR = new JsonTextReader(R))
                return JSS.Deserialize<T>(RR);
        }
        public string Serialize(object obj)
        {
            
            using (var R = new StringWriter())
            using (var RR = new JsonTextWriter(R))
            {
                RR.QuoteName = false;
                JSS.Serialize(RR, obj);
                return R.ToString();
            }
        }
    }
    public interface IRefObject
    {
    }
    public class JsonRefConverter : JsonConverter
    {
        protected Dictionary<int, object> Storage = new Dictionary<int, object>();
        public override bool CanConvert(Type objectType)
        {
            return typeof(object).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (existingValue is int)
            {
                object x;
                Storage.TryGetValue((int)existingValue, out x);
                return x;
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var key = RuntimeHelpers.GetHashCode(value);
            if (Storage.ContainsKey(key))
            {
                writer.WriteValue(key);
                return;
            } 
            Storage.Add(key, value);
            Type type = value.GetType();
            writer.WriteStartObject();
            writer.WritePropertyName("$id");
            writer.WriteValue(key);

            writer.WritePropertyName("$type");
            serializer.Serialize(writer, type);

            foreach (PropertyInfo prop in type.GetProperties())
            {
                object propVal = prop.GetValue(value, null);
                if (propVal != null)
                {
                    writer.WritePropertyName(prop.Name);
                   // writer.WriteValue(propVal);
                    serializer.Serialize(writer,propVal);
                }
            }
            writer.WriteEndObject();
        }
    }

    public class JsonTypeConverter : JsonConverter
    {
        protected List<Type> Types = new List<Type>();
        public override bool CanConvert(Type objectType)
        {
            return typeof(Type).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = value as Type;
            var I=Types.IndexOf(type);
            if (I >= 0) writer.WriteValue(I);
            else
            {
                Types.Add(type);
                writer.WriteStartObject();
                writer.WritePropertyName("id");
                writer.WriteValue(Types.Count - 1);
                writer.WritePropertyName("methods");
                writer.WriteStartArray();
                foreach (var M in type.GetMethods())
                    writer.WriteValue(M.Name);
                writer.WriteEndArray();

                writer.WritePropertyName("events");
                writer.WriteStartArray();
                foreach (var M in type.GetEvents())
                    writer.WriteValue(M.Name);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }
    }

    public class JsonRPCMessageConverter : JsonConverter
    {
        List<Type> MessageType = new List<Type>() { typeof(RPCEventMessage),typeof(RPCBindMessage),typeof(RPCInvokeMessage) };
        public RPCSocketClient Client { get; set; }
        public override bool CanConvert(Type objectType)
        {
           return typeof(RPCMessage).IsAssignableFrom(objectType);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            var type = MessageType[jObject.Value<int>("type")];
            var obj = Activator.CreateInstance(type) as RPCMessage;
            obj.Client = Client;
            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), obj);

            return obj;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {           
            Type type = value.GetType();
            writer.WriteStartObject();

            writer.WritePropertyName("type");
            serializer.Serialize(writer, MessageType.IndexOf(type));
            foreach (PropertyInfo prop in type.GetProperties())
            {
                object propVal = prop.GetValue(value, null);
                if (propVal != null)
                {
                    writer.WritePropertyName(prop.Name);
                    // writer.WriteValue(propVal);
                    serializer.Serialize(writer, propVal);
                }
            }
            writer.WriteEndObject();
        }
    }
}
