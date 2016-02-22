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
    public class RPCSocketClient: WSClient 
    {
        protected BinaryConverter Converter;
        private object qwe= new object();
        private object qwe2 = new object();
        public RPCSocketClient()  {
            Converter = new BinaryConverter();
            Converter.FunctionCall += (fn,arg) => {
                lock(qwe2)
                Send(new RPCCallBackMessage() { arg=arg,fn=fn  });
            };
        }
        public void Send(RPCMessage message)
        {
            var len = Converter.CheckSize(message);
            var offset = GetResponseHeaderSize(len);
            var data = new byte[len + offset];
            WriteResponseHeader(data, len);
            Converter.Write(data, message, ref offset);
            SendRaw(data);
        }
        protected override void onMessage(byte[] message)
        {
            lock (qwe)
            {
                Converter.Process(message);
            }
            //try
            //{
               
            //}
            //catch (Exception E)
            //{
            //    //Send(new RPCEventMessage() { member = "Error", args = new object[] { E.Message } });
            //}
        }
    }
}
