using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using WebTools.WebSocket;
using System.IO;
using WebTools.Helpers;

namespace WebTools.RPC
{   
    public class RPCSocketHandler : WSHandler
    {
        public static string wsClient
        {
            get
            {
                return Properties.Resources.BinaryConvert + Properties.Resources.Client;
            }
        }

        protected override WSClient CreateClient(Stream stream, byte[] request)
        {
            return new RPCSocketClient(stream);
        }
        public void FireEvent(string Name, object Data)
        {
            //Send(c, "", Name, Serializer.Serialize(Data));
        }
        private void Send(string key, string handlerid,string body)
        {
            Send(StreamHelpers.Join(key," ",handlerid," ",body));
        }
    }
}
