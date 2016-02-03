using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin;
 

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            WebApp.Start("http://localhost:8080", InitWebApp);
            Console.ReadLine();
        }
        static void InitWebApp(IAppBuilder Builder)
        {
            Builder.Use(Handle);
        }
        static Task Handle(IOwinContext ctx, Func<Task> next)
        {
            return Task.Run(() =>
            {
                ctx.Response.Write("Hello World of " + ctx.Request.QueryString);
            });
        }
    }
}
