using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class HttpFile
    {
        public string Name;
        public string Type;
        public byte[] Content;

        public HttpFile(string path)
        {
            this.Content = Encoding.UTF8.GetBytes(File.ReadAllText(path));
            this.Name = Path.GetFileName(path);
            this.Type = GetMimeType(path);
        }
        public HttpFile(string name,string content)
        {
            this.Content = Encoding.UTF8.GetBytes(content);
            this.Name = name;
            this.Type = GetMimeType(name);
        }
        public static string GetMimeType(string Path)
        {
            var id = Path.LastIndexOf(".");
            switch (Path.Substring(id + 1))
            {
                case "js": return "application/x-javascript";
                case "css": return "text/css";
                case "html": return "text/html";
                default: return "";
            }
        }
    }
}
