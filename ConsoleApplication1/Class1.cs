using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{


    public class Proxy : RealProxy
    {
        public static T Create<T>(T Obj)
        {
            return (T)new Proxy(Obj).GetTransparentProxy();
        }

        protected object Obj;
        public Proxy(object Obj):base(Obj.GetType())
        {
            this.Obj = Obj;
        }
        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;
            var result = methodInfo.Invoke(Obj, methodCall.InArgs);
            Console.WriteLine("----");
            Console.WriteLine(methodInfo.Name + " " + string.Join(", ", methodCall.InArgs));
            return new ReturnMessage(result, null, 0,
              methodCall.LogicalCallContext, methodCall);
        }
    }
}
