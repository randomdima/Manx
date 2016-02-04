using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebTools.File
{
    public class HtmlFile:HttpFile
    {
        public HtmlFile(string Path,string Title,List<HttpFile> Scripts){

            var scriptsRef = string.Join("\n", Scripts.Select(q=>"<script class='text/javascript' src='"+q.Path+"'></script>"));
            this.Content = Encoding.UTF8.GetBytes(@"<html><head>" + scriptsRef + "</head><body></body></html>");
            this.Type = "text/html";
            this.SetPath(Path);
        }
    }
}
