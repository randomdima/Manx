using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebTools.File
{
    public class HttpFile 
    {
        public string Path;
        public string Type;
        public byte[] Content;

        protected HttpFile() { }
        public HttpFile(string path)
        {
            Content = Encoding.UTF8.GetBytes(System.IO.File.ReadAllText(path));           
            Type = MimeMapping.GetMimeMapping(path);
            SetPath(path);
        }
        public HttpFile(string path, string content, string type = null)
        {
            Content = Encoding.UTF8.GetBytes(content);
            Type = type;
            SetPath(path);
        }

        protected void SetPath(string path)
        {      
            Path = path;
            Path = Path.Replace("\\", "/").Replace("..","up");
            if (!Path.StartsWith("/")) Path = "/" + Path;
            if (Type == null) Type = MimeMapping.GetMimeMapping(Path);
        }
        public void ProcessRequest(HttpListenerContext Context)
        {
            Context.Response.StatusCode = 200;
            Context.Response.ContentLength64 = Content.Length;
            Context.Response.ContentType = Type;
            Context.Response.OutputStream.Write(Content, 0, Content.Length);
        }
    }
}
