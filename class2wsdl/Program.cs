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
            string assemblyStr;
            string classStr;
            if (args.Length == 2)
            {
                assemblyStr = args[0];
                classStr = args[1];
            }
            else
            {
                Console.WriteLine("\n2 parameters are required\n\n" +
                    "Usage:\nclass2wsdl.exe [compiled program or class library without extension] [class name]\n\n" +
                    "Example:\nGiven Class1 in ClassLibrary.dll:\n" +
                    "class2wsdl.exe ClassLibrary Class1");
                return;
            }

            new WSDLGenerator(assemblyStr, classStr).Run();
            Console.WriteLine("Done");
        }
    }
}
