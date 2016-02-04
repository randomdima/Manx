using Fleck;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 

namespace Server.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileServer = new FileServer("http://localhost:8080/");
            fileServer.Add(new HttpFile("index.html","AAAAAAAAAAA<b>AAAAAAAAAA</b>AAAAAAAAAAAA"));
            fileServer.Add(new HttpFile("..//..//Scripts//OnLoad.js"));
            fileServer.Start();

            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.Start(socket =>
            {
                socket.OnOpen = () => Console.WriteLine("Open!");
                socket.OnClose = () => Console.WriteLine("Close!");
                socket.OnMessage = message => socket.Send(message);
            });
            
            Console.WriteLine("Listening...");
            Console.ReadLine();
        }
    }
}
