using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            qwe real = new qwe();
            real.x = 2;
            Console.WriteLine(real.x);

            real = Proxy.Create(real);

            Console.WriteLine(real.x);
            real.x++;
            Console.WriteLine(real.x);


            Console.ReadKey();

        }
    }


    public class qwe:MarshalByRefObject
    {
       public int x{get;set;}
    }
}
