using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace class2wsdl
{
    class Program
    {
        static void Main(string[] args)
        {
            String classStr = "";
            if (args.Length == 1)
            {
                classStr = args[0];
            }
            else
            {
                Console.WriteLine("1 parameter is required\n" +
                    "Usage: class2wsdl.exe class");
                return;
            }

            new WSDLGenerator(classStr).Run();
        }
    }
}
