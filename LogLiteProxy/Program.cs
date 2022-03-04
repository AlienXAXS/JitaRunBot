using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogLiteProxy
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Startup Parameters: ");
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }

            Console.ReadLine();
        }
    }
}
