using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public interface IHttpListenerHandler
    { 
        void ProcessRequest(HttpListenerContext Context);
    }
    public class HttpServer:IDisposable
    {
        private Task ListenerTask;
        private HttpListener Listener= new HttpListener();
        private Dictionary<string, IHttpListenerHandler> Handlers = new Dictionary<string, IHttpListenerHandler>();

        public HttpServer(string URL)
        {
            Listener.Prefixes.Add(URL);
            Start();            
        }

        public void Add(string Path,IHttpListenerHandler Handler)
        {
            if (Path.StartsWith("/")) Path = "/" + Path;
            Handlers.Add(Path, Handler);
        }
        public void Start()
        {
            Listener.Start();
            ListenerTask = Task.Run(()=>Handle());
        }
        public void Stop()
        {
            Listener.Stop();
            ListenerTask.Dispose();
            ListenerTask = null;
        }
        public void Dispose()
        {
            Stop();
        }

        private void Handle()
        {
            while (true)
            {
                var ctx = Listener.GetContext();
                Task.Run(() => ProcessRequest(ctx));
            }
        }       
        private void ProcessRequest(HttpListenerContext Context)
        {
            IHttpListenerHandler file;
            if (Handlers.TryGetValue(Context.Request.Url.LocalPath, out file))
            {
                try
                {
                    file.ProcessRequest(Context);
                }
                catch (Exception E)
                {
                    Context.Response.StatusCode = 500;
                    var Message = Encoding.UTF8.GetBytes("Exception: "+E.Message);
                    Context.Response.ContentLength64 = Message.Length;
                    Context.Response.OutputStream.Write(Message, 0, Message.Length);
                }
            }
            else
            {
                Context.Response.StatusCode = 404;
                var Message = Encoding.UTF8.GetBytes("Not found");
                Context.Response.ContentLength64 = Message.Length;
                Context.Response.OutputStream.Write(Message, 0, Message.Length);
            }
        }
    }
}
