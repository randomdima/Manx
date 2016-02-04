using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.File
{
    public class HttpServer:IDisposable
    {
        private Task ListenerTask;
        private HttpListener Listener= new HttpListener();
        private Dictionary<string, HttpFile> Files = new Dictionary<string, HttpFile>();

        public HttpServer(string URL)
        {
            Listener.Prefixes.Add(URL);
            Start();            
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
           
            Files.Add(Handler.Path, Handler);
            return Handler;
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
            HttpFile file;
            if (Files.TryGetValue(Context.Request.Url.LocalPath, out file))
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
