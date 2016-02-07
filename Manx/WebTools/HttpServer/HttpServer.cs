using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebTools.Helpers;

namespace WebTools.HttpServer
{
    public class HttpServer:IDisposable
    {
        private static UTF8Encoding Encoder = new UTF8Encoding(false, false);
        private static byte SpaceKey = Encoder.GetBytes(" ")[0];
        
        private TcpListener Listener;
        private Dictionary<string, IHttpHandler> Handlers;
        private Thread MainThread;

        public HttpServer(string url = null, int port = 8080)
        {
            Handlers = new Dictionary<string, IHttpHandler>();
            Listener = new TcpListener(url==null?IPAddress.Any:IPAddress.Parse(url), port);
        }


        private static byte[] HttpHeaderA = Encoder.GetBytes("HTTP/1.1 ");
        private static byte[] HttpHeaderB = Encoder.GetBytes(" OK\r\nContent-Type: ");
        private static byte[] HttpHeaderC = Encoder.GetBytes("\r\nContent-Length: ");
        private static byte[] HttpHeaderE = Encoder.GetBytes("\r\n\r\n");
        public static byte[] GetHttpResponse(string code, string type, byte[] content)
        {
            return StreamHelpers.Join(HttpHeaderA,
                                      Encoder.GetBytes(code),
                                      HttpHeaderB,
                                      Encoder.GetBytes(type),
                                      HttpHeaderC,
                                      Encoder.GetBytes(content.Length.ToString()),
                                      HttpHeaderE,
                                      content);
        }
        public static byte[] GetHttpResponse(string code, string type, string content)
        {
            return GetHttpResponse(code, type, Encoder.GetBytes(content));
        }
        public void Start()
        {
            Dispose();
            Listener.Start();
            ThreadPool.SetMinThreads(2, 2);
            ThreadPool.SetMaxThreads(100, 100);
            MainThread = new Thread(()=> {
                while (true) ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessRequest), Listener.AcceptTcpClient());                
            });
            MainThread.Start();
        }
        public void Stop()
        {
            if (MainThread != null) MainThread.Abort();
            Listener.Stop();
        }
        public void Dispose()
        {
            Stop();
        }
        protected void ProcessRequest(Object StateInfo)
        {
            var client = StateInfo as TcpClient;
            //client.NoDelay = true;
            var stream = client.GetStream();
            var req = stream.Read();                   
            var q = 3; //Skip "GET "
            if (req.Length < 6 || req[0] == 0)
            {
                stream.Close();
                return;
            }
            while (req.Length>++q && req[q] != SpaceKey);
            ProcessRequest(Encoder.GetString(req, 4, q - 4), stream, req);
            stream.Close();
            client.Close();
        }
        protected void ProcessRequest(string path, Stream stream, byte[] request)
        {            
            IHttpHandler handler;
            Console.WriteLine(path);
            if (Handlers.TryGetValue(path, out handler))
            {
                try
                {
                    handler.Handle(stream, request);
                }
                catch (Exception E)
                {
                    Console.WriteLine("--------------");
                    Console.WriteLine(E.Message);
                    stream.Write(GetHttpResponse("500", "text/html", E.Message));
                }
            }
            else
            {
                stream.Write(GetHttpResponse("404", "text/html", "Not found"));
            }
        }


        public List<HttpFile> AddFolder(string Path, string Pattern = "*")
        {
           return Directory.EnumerateFiles(Path, Pattern, SearchOption.AllDirectories)
                           .OrderBy(q => q).Select(q => Add(q)).ToList();
        }
        public HttpFile Add(string Path)
        {
            return Add(new HttpFile(Path));
        }
        public HttpFile Add(string Path, string Content,string Type=null)
        {
            return Add(new HttpFile(Path, Content, Type));
        }
        public HttpFile Add(HttpFile Handler)
        {           
            return Add(Handler.Path, Handler) as HttpFile;
        }
        public IHttpHandler Add(string Path,IHttpHandler Handler)
        {
            if (!Path.StartsWith("/")) Path = "/" + Path;
            Handlers.Add(Path, Handler);
            return Handler;
        }
    }
}
