using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WebTools.HttpServer;
using WebTools.RPC;
using WebTools.WebSocket;


namespace Server.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            using (HttpServer server = new HttpServer(port:12397))
            {
                var files = server.AddFolder("..//..//Scripts", "*.js");
                server.Add(new HtmlFile("", "Manx", files));

                var WS = new WSHandler();
                server.Add("ws",WS);
                server.Start();
                WS.OnConnect += q =>
                {
                    q.OnMessage += m => {
                        var qm = m.Replace("\"x\"", "_y").Replace("\"y\"", "_x").Replace("\"ly\"", "_lx").Replace("\"lx\"", "_ly")
                                   .Replace("_x", "\"x\"").Replace("_y", "\"y\"").Replace("_lx", "\"lx\"").Replace("_ly", "\"ly\"");
                        WS.Send(m);
                        WS.Send(qm);
                    };
                };
                Console.WriteLine("Listening...");
                Console.ReadLine();
            }
        }

        [RPCMember]
        public static void SetPosition(int x,int y)
        {
           // wsServer.FireEvent("PositionChanged",new {x,y});
        }
    }
}
