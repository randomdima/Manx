using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WebTools.Helpers;
using WebTools.WebSocket;
using System.Reflection;
using HandlerType = System.Func<System.Collections.Generic.Dictionary<string, object>, object>;
using System.Collections;

namespace WebTools.RPC
{
    public class RPCHandler
    {
        public string Path { get; set; }
        public HandlerType Handler { get; set; }
    }

    public class RPCSocketClient: WSClient 
    {
        public readonly JsonRefSerializer Json;
        public RPCSocketClient(Stream stream) : base(stream) {
            Json = new JsonRefSerializer(this);
        }
        public void Start(object root)
        {
            new RPCEventMessage() { Client = this, member = "Start", args = new object[] { root } }.Send();
        }
        public void Send(RPCMessage message)
        {
            Send(Json.Serialize(message));
        }

        protected override void onMessage(object message)
        {
            try
            {
                var msg = Json.Deserialize<RPCMessage>(message.ToString());
                msg.Process();
            }
            catch (Exception E)
            {
                new RPCEventMessage() { Client=this,member="Error",args=new object[]{E.Message} }.Send();
            }
        }

    }
}
