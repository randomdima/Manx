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
using WebTools.Binary;

namespace WebTools.RPC
{
    public class RPCHandler
    {
        public string Path { get; set; }
        public HandlerType Handler { get; set; }
    }

    public class RPCSocketClient: WSClient 
    {
        protected BinaryConverter Converter;
        public RPCSocketClient(Stream stream) : base(stream) {
            Converter = new BinaryConverter();
        }
        public void Start(object root)
        {
            Send(root);
            //new RPCEventMessage() { Client = this, member = "Start", args = new object[] { root } }.Send();
        }
        public void Send(object message)
        {
            var len = Converter.CheckSize(message);
            var offset = GetResponseHeaderSize(len);
            var data = new byte[len + offset];
            WriteResponseHeader(data, len);
            Converter.Write(data, message, ref offset);
            SendRaw(data);
        }

        protected override void onMessage(object message)
        {
            try
            {
                new RPCInvokeMessage() { Client = this, member = "Click" }.Send();
               //var msg = Converter.Convert<RPCMessage>(message as byte[]);
               //msg.Process();
            }
            catch (Exception E)
            {
                new RPCEventMessage() { Client=this,member="Error",args=new object[]{E.Message} }.Send();
            }
        }

    }
}
