using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebTools.WebSocket
{
    public class WSServer:IDisposable
    {
        protected TcpListener Listener;
        public WSServer(string url = "127.0.0.1", int port = 8181)
        {
            Listener = new TcpListener(IPAddress.Parse(url), port);
        }
        public void Start()
        {
            Listener.Start();
            Task.Run(() =>
            {
                while (true) 
                    new WSClient().Listen(Listener.AcceptTcpClient());
            });
        }
        public void Stop()
        {
            Listener.Stop();
        }
        public void Dispose()
        {
            Stop();
        }
    }
}
