using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebTools.Binary;
using WebTools.Helpers;

namespace WebTools.RPC
{
    public abstract class RPCMessage
    {
        [Ignore]
        public abstract void Process(RPCSocketClient Client);
    }
    public sealed class RPCRootMessage: RPCMessage
    {
        public Type[] MessageTypes;
        public object Root;
        public RPCRootMessage(){
            MessageTypes = new Type[] {  };
        }
        public RPCRootMessage(object root):this(){
            Root = root;
        }
        public override void Process(RPCSocketClient Client)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class RPCCallBackMessage: RPCMessage
    {
        public object fn;
        public object[] arg;
        public override void Process(RPCSocketClient Client)
        {
           
        }
    }   
}
