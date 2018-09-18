using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace class2wsdl
{
    class WSDLGenerator
    {
        //https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection
        Assembly classA;
        Module[] classMods;
        MethodInfo[] methods;
        String wsdlStr;
        
        public WSDLGenerator(String classStr)
        {
            classA = Assembly.Load(classStr);
            classMods = classA.GetModules();
            wsdlStr = classA.GetName() + ".wsdl";
        }

        public void Run()
        {
            //no sé cuántos módulos hay...creo que es uno por clase?
            foreach(Module mod in classMods)
            {
                methods.Concat(mod.GetMethods()); //extraer métodos
            }
            ///TODO Extraer parámetros de cada método
            ///TODO Escribir en el archivo
            WriteWSDL();
        }

        void WriteWSDL()
        {
            //opción 1: escribir línea por línea
            //StreamWriter wsdlFile = new StreamWriter(wsdlStr);
            //wsdlFile.WriteLine("<?xml version=\"1.0\"?>");

            //opción 2: generar XML
            //var xml = new XElement(...
        }
    }
}
