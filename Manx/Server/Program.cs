using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTools.File;
using WebTools.RPC;
using WebTools.WebSocket;
 

namespace Server.Tools
{
    class Program
    {
        static WSServer wsServer = new WSServer();
        static HttpServer fileServer =  new HttpServer("http://localhost:8080/");
        static void Main(string[] args)
        {
            //wsServer.Add(typeof(Program));
            //wsServer.Start();

            var files = fileServer.AddFolder("..\\..\\Scripts", "*.js");
            //files.Add(fileServer.Add("Client.js", RPCServer.Client));
            fileServer.Add(new HtmlFile("", "Manx", files));
            fileServer.Start();

            wsServer.Start();
            
            Console.WriteLine("Listening...");
            Console.ReadLine();
            wsServer.Stop();
        }

        [RPCMember]
        public static void SetPosition(int x,int y)
        {
           // wsServer.FireEvent("PositionChanged",new {x,y});
        }
    }
}
