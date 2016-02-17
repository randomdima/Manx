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
    public class JsFile:HttpFile 
    {
        public static string Combine(string Path){
            return String.Join("\r\n",Directory.EnumerateFiles(Path, "*.js", SearchOption.AllDirectories).Where(q=>!q.EndsWith(".min.js"))
                                               .OrderBy(q => q).Select(q => File.ReadAllText(q)));
        }
    }
}
