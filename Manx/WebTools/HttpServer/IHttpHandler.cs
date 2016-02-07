using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.HttpServer
{
    public interface IHttpHandler
    {
        void Handle(Stream stream,byte[] request);
    }
}
