using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebTools.HttpServer
{
    public class HtmlFile:HttpFile
    {
        public HtmlFile(string Path,string Title,List<HttpFile> Scripts){

            var scriptsRef = string.Join("\n", Scripts.Select(q=>"<script class='text/javascript' src='"+q.Path+"'></script>"));
            SetContent("text/html","<html><head>" + scriptsRef + "</head><body></body></html>");
            this.SetPath(Path);
        }
    }
}
