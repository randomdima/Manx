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
            Console.WriteLine(real.GetHashCode());
            Console.WriteLine(real.GetHashCode());
            real = new qwe();
            Console.WriteLine(real.GetHashCode());
            Console.ReadKey();

        }
    }


    public class qwe:MarshalByRefObject
    {
       public int x{get;set;}
    }
}
