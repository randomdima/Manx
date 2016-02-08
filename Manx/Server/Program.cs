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
            using (HttpServer server = new HttpServer(port:12397))
            {
                var files = server.AddFolder("Scripts", "*.js");
                server.Add(new HtmlFile("", "Manx", files));

                var WS = new WSHandler();
                server.Add("ws",WS);
                server.Start();
                WS.OnConnect += q =>
                {
                    q.OnMessage += m => { WS.Send(m); };
                };
                Random x=new Random();
                Timer T = new Timer();
                T.Interval = 1000;
                T.Elapsed += (q,w)=> WS.Send("{x:"+x.Next(2000)+ ",y:" + x.Next(1000) + ",c:'green'}");
                T.Start();
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
