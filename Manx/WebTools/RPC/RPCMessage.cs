using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebTools.Helpers;

namespace WebTools.RPC
{
    public abstract class RPCMessage
    {
        public RPCSocketClient Client;
        public abstract void Process();
        public void Send()
        {
            Client.Send(this);
        }
    }
    public class RPCEventMessage : RPCMessage
    {
        public object obj { get; set; }
        public string member { get; set; }
        public object[] args { get; set; }
        public override void Process()
        {
            obj.GetType().GetEvent(member).RaiseMethod.Invoke(obj, args);
        }
    }
    public class RPCBindMessage : RPCMessage
    {
        public object obj { get; set; }
        public string member { get; set; }
        public override void Process()
        {
            obj.GetType().GetEvent(member).AddEventHandler(obj,
                (Action<object>)(q =>
                {
                    new RPCEventMessage()
                    {
                        Client=Client,
                        obj = obj,
                        member = member,
                        args = new object[] { q }
                    }.Send();
                }));
        }
    }
    public class RPCInvokeMessage:RPCMessage
    {
        public object obj { get; set; }
        public string member { get; set; }
        public object[] args { get; set; }
        public static object GetDefaultValue(Type Type)
        {
            if (Type.IsValueType)
            {
                return Activator.CreateInstance(Type);
            }
            return null;
        }
        protected void ConvertArgs(ParameterInfo[] Params)
        {
            var res = new object[Params.Length];
            for (var q = 0; q < Params.Length; q++)
            {
                if (res.Length <= q)
                    res[q] = GetDefaultValue(Params[q].ParameterType) ?? Params[q].DefaultValue;
                //else res[q] = Client.Json.ConvertToType(args[q], Params[q].ParameterType) ?? Params[q].DefaultValue;
            }
            args = res;
        }
        public override void Process()
        {
            var M = obj.GetType().GetMethod(member);
            ConvertArgs(M.GetParameters());
            M.Invoke(obj, args);
        }
    }
}
