using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebTools.Helpers;

namespace WebTools.HttpServer
{
    public class HttpFile:IHttpHandler 
    {
        public string Path;
        public byte[] Content;

        protected HttpFile() { }
        public HttpFile(string path)
        {
            SetContent(MimeMapping.GetMimeMapping(path),File.ReadAllText(path));
            SetPath(path);
        }
        public HttpFile(string path, string content, string type = null)
        {
            SetContent(type??MimeMapping.GetMimeMapping(path), content);
            SetPath(path);
        }

        public void SetContent(string Type, string content)
        {
            Content = HttpServer.GetHttpResponse("200", Type, content);
        }
        public void SetContent(string Type,byte[] content)
        {
            Content = HttpServer.GetHttpResponse("200", Type, content);
        }
        protected void SetPath(string path)
        {      
            Path = path;
            Path = Path.Replace("\\", "/").Replace("..","up");
            if (!Path.StartsWith("/")) Path = "/" + Path;
        }
        public void Handle(Stream stream,byte[] request)
        {
            stream.Write(Content);
            stream.Flush();
        }
    }
}
