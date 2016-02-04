using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using HandlerType = System.Func<System.Collections.Generic.Dictionary<string, object>, object>;

namespace WebTools.RPC
{
    public class RPCMember : Attribute
    {
        public string Name { get; set; }
        public RPCMember(string Name = null)
        {
            this.Name = Name;
        }
        public HandlerType CreateHandler(MemberInfo Member)
        {
            return Member.GetHandler(); 
        }

    }
}
