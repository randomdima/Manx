using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WebTools.RPC
{
    public class JsonRefSerializer:JavaScriptSerializer
    {
        public static readonly JavaScriptSerializer simple = new JavaScriptSerializer();
        private readonly JsonObjectStorage ObjectStorage;
        private readonly JsonTypeStorage TypeStorage;
        public JsonRefSerializer(RPCSocketClient Client)
        {
            ObjectStorage = new JsonObjectStorage();
            TypeStorage = new JsonTypeStorage();
            RegisterConverters(new List<JavaScriptConverter>() {
                TypeStorage,
                ObjectStorage,
                new JsonMessageConverter(Client) });
        }
        public object GetObject(int key)
        {
            return ObjectStorage.Get(key);
        }
    }

    public class JsonObjectStorage : JavaScriptConverter
    {
        private Dictionary<int, object> Storage = new Dictionary<int, object>();
        public static List<Type> Types = new List<Type>() { typeof(object) };
       
        public object Get(int key)
        {
            object obj;
            Storage.TryGetValue(key, out obj);
            return obj;
        }
        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            object key = 0;
            if (!dictionary.TryGetValue("_id", out key)) return serializer.ConvertToType(dictionary, type);
            return Get((int)key);
        }
        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            if (!Types.Contains(obj.GetType()))
                return obj as IDictionary<string, object>;
            if (obj == null) return null;

            var id = RuntimeHelpers.GetHashCode(obj);
            var res=new Dictionary<string,object>();
            res.Add("_id", id); 
            if (!Storage.ContainsKey(id))
                Storage.Add(id, obj);
            else return res;
            res.Add("_type", obj.GetType());
            var props = obj.GetType().GetProperties();
            foreach (var p in props)
            {
                var v = p.GetValue(obj);
                if (v != null)
                    res.Add(p.Name, v);
            }
            return res;
        }
        public override IEnumerable<Type> SupportedTypes
        {
            get { return JsonObjectStorage.Types; } }
        }
    
    public class JsonTypeStorage : JavaScriptConverter
    {
        private Dictionary<string, Type> Storage = new Dictionary<string, Type>();

        public Type Get(string key)
        {
            Type obj;
            Storage.TryGetValue(key, out obj);
            return obj;
        }
        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            return Storage[(string)dictionary["type"]];
        }
        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            var t = obj as Type;
            if (t == null) return null;
            var res = new Dictionary<string, object>();
            res.Add("name", t.Name);
            if (Storage.ContainsKey(t.Name))
                return res;
            Storage.Add(t.Name, t);
            res.Add("methods", t.GetMethods().Select(q => q.Name).ToArray());
            res.Add("events", t.GetEvents().Select(q => q.Name).ToArray());
            return res;
        }

        public override IEnumerable<Type> SupportedTypes
        {
            get { return new List<Type>() { typeof(Type) }; }
        }
    }

    public class JsonMessageConverter : JavaScriptConverter
    {
        protected static List<Type> Types = new List<Type>() { typeof(RPCInvokeMessage), typeof(RPCBindMessage), typeof(RPCEventMessage), typeof(RPCMessage) };
        protected RPCSocketClient Client;
        public JsonMessageConverter(RPCSocketClient Client){
         this.Client=Client;
        }
        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            var T = Types[(int)dictionary["type"]];
            RPCMessage M = JsonRefSerializer.simple.ConvertToType(dictionary, T) as RPCMessage;
            M.Client = Client;
            return M;
        }
        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            if (obj == null) return null;
            var res = new Dictionary<string, object>();
            var props = obj.GetType().GetProperties();
            foreach (var p in props)
            {
                var v = p.GetValue(obj);
                if (v != null)
                    res.Add(p.Name, v);
            }
            res.Add("type", Types.IndexOf(obj.GetType()));
            return res;
        }

        public override IEnumerable<Type> SupportedTypes
        {
            get { return Types; }
        }
    }

}
