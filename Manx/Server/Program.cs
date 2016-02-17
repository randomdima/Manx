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
                files.Add(server.Add("..//..//..//WebTools//JavaScript//Client.min.js"));
                server.Add(new HtmlFile("", "Manx", files));
                var global = new World();
                global.Add(100,100,10,"red");

                var WS = new RPCSocketHandler();
                WS.OnConnect += q =>
                {
                    (q as RPCSocketClient).Start(global);
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
        public List<Child> Items {get;set;}
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
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; set; }
        public string Color { get; set; }
        public event Action<int,int> OnMove;
        public void MoveTo(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
            if (OnMove != null) OnMove(X, Y);
        }
    }
}
