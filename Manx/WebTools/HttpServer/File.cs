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
        public string FilePath;
        public string Path;
        public byte[] Content;

        protected HttpFile() { }
        public HttpFile(string path)
        {
            SetContent(MimeMapping.GetMimeMapping(path),File.ReadAllText(path));
            FilePath = path;
            SetPath(path);
        }
        public HttpFile(string path, string content, string type = null)
        {
            SetContent(type??MimeMapping.GetMimeMapping(path), content);
            SetPath(path);
        }

        public void SetContent(string Type, string content)
        {
            Content = HttpServer.BuildHttpResponse("200", Type, content);
        }
        public void SetContent(string Type,byte[] content)
        {
            Content = HttpServer.BuildHttpResponse("200", Type, content);
        }
        protected void SetPath(string path)
        {      
            Path = path;
            Path = Path.Replace("\\", "/").Replace("..","up");
            if (!Path.StartsWith("/")) Path = "/" + Path;
        }
        public void Handle(Stream stream,byte[] request)
        {
#if DEBUG
            if(FilePath!=null)  SetContent(MimeMapping.GetMimeMapping(FilePath),File.ReadAllText(FilePath));
#endif
            stream.Write(Content);
            
           // stream.Flush();
        }
    }
}
