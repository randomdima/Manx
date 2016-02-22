using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using WebTools.Binary;
using WebTools.HttpServer;
using WebTools.RPC;
using WebTools.WebSocket;


namespace Server.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            using (HttpServer server = new HttpServer(port:12397))
            {
                var files = server.AddFolder("..//..//Scripts", "*.js");
                files.Add(server.Add("..//..//..//WebTools//JavaScript//0_Binary.js"));
                files.Add(server.Add("..//..//..//WebTools//JavaScript//2_wsClient.js"));
                server.Add(new HtmlFile("", "Manx", files));
                
                                
                var global = new World();


                global.Add(100, 100, 20, "red");
                global.Add(200, 100, 20, "green");
                global.Add(100, 200, 20, "blue");
                var WS = new RPCSocketHandler();
                WS.OnConnect += q =>
                {
                    q.Send(new RPCRootMessage(global.Items[0]));
                };
                server.Add("ws", WS);
                server.Start();
                Console.WriteLine("Listening...");
                Console.ReadLine();
            }
        }
    }
    class World
    {
        public List<Child> Items;
        public event Action<Child> ItemAdded;
        public event Action<Child> ItemRemoved;
        public World()
        {
            Items = new List<Child>();
        }
        public void Add(int X, int Y,int Size, string Color)
        {
            var newone = new Child() { X = X, Y = Y, Size = Size, Color = Color };
            Items.Add(newone);
            if (ItemAdded != null) ItemAdded(newone);
        }
        public void Remove(Child Item)
        {
            Items.Remove(Item);
            if (ItemRemoved != null) ItemRemoved(Item);
        }
    }
    class Child
    {
        public int X;
        public int Y;
        public int Size;
        public string Color;
        //public event Action<int,int> OnMove;
        public void MoveTo(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
            //   if (OnMove != null) OnMove(X, Y);
        }
    }
}
