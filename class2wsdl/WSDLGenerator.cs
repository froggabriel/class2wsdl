using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace class2wsdl
{
    class WSDLGenerator
    {
        //https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection
        Assembly _assembly;
        Type _classType;
        MethodInfo[] _methods;
        readonly string _wsdlStr;
        
        public WSDLGenerator(string assemblyStr, string classStr)
        {
            try
            {
                this._assembly = Assembly.Load(assemblyStr);
                Console.WriteLine("Loaded assembly: " + assemblyStr);
            } catch (FileNotFoundException e) {
                Console.WriteLine(e.StackTrace);
            }
            this._classType = this._assembly.GetType(classStr);
            Console.WriteLine("Got class type: " + this._classType.Name);
            this._wsdlStr = classStr + ".wsdl";
        }

        public void Run()
        {
            this._methods = this._classType.GetMethods(); //extraer métodos
            Console.WriteLine("Extracted methods:");
            foreach (MethodInfo meth in this._methods)
            {
                Console.WriteLine(meth.Name);
            }

            this.WriteWSDL();
        }

        private void WriteWSDL()
        {
            XmlWriter xmlWriter = XmlWriter.Create(this._wsdlStr);

            XNamespace xmlns = "http://schemas.xmlsoap.org/wsdl/";
            XNamespace xsd;
            XNamespace soap;
            XElement definitions;
            XElement types;
            XElement portType;
            XElement binding;
            XElement service;

            //write definitions
            definitions = new XElement(
                xmlns + "definitions", //indica que definitions está dentro del namespace xmlns
                new XAttribute("name", this._classType.Name),
                new XAttribute("targetNamespace", "urn:" + this._classType.Name),
                new XAttribute(XNamespace.Xmlns + "wsdl", "http://schemas.xmlsoap.org/wsdl/"),
                new XAttribute(XNamespace.Xmlns + "soap", "http://schemas.xmlsoap.org/wsdl/soap/"),
                new XAttribute(XNamespace.Xmlns + "tns", "urn:" + this._classType.Name),
                new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                new XAttribute(XNamespace.Xmlns + "SOAP-ENC", "http://schemas.xmlsoap.org/soap/encoding/"),
                new XAttribute("xmlns", "http://schemas.xmlsoap.org/wsdl/")
                );

            //write types (Tipos complejos)
            types = new XElement(
                xmlns + "types",
                new XAttribute("xmlns", "http://schemas.xmlsoap.org/wsdl/")
                );

            foreach(MethodInfo m in _methods) {
                types.Add();//agregar elemento para cada método
            }

            definitions.Add(types);

            // write messages (Mensajes para comunicarse con la clase)
            //foreach
            definitions.Add(new XElement(
                xmlns + "message"
                ));

            // write portType (Puerto para comunicar con la clase)
            portType = new XElement(
                xmlns + "portType"
                );

            definitions.Add(portType);

            // write binding (Vinculación de los llamados con el transporte - document, SOAP over HTTP)
            binding = new XElement(
                xmlns + "binding"
                );

            definitions.Add(binding);

            // write service (Punto de comunicación con la clase HolaMundo)
            service = new XElement(
                xmlns + "service"
                );

            definitions.Add(service);

            //otra forma, pero me estaba causando problemas
            /*xmlWriter.WriteStartElement("definitions");
            xmlWriter.WriteAttributeString("name", this._classType.Name);
            xmlWriter.WriteAttributeString("targetNamespace", "urn:" + this._classType.Name);
            xmlWriter.WriteAttributeString("xmlns","wsdl", null, "http://schemas.xmlsoap.org/wsdl/");
            xmlWriter.WriteAttributeString("xmlns", "soap", null, "http://schemas.xmlsoap.org/wsdl/soap/");
            xmlWriter.WriteAttributeString("xmlns", "tns", null, "urn:" + this._classType.Name);
            xmlWriter.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
            xmlWriter.WriteAttributeString("xmlns", "SOAP-ENC", null, "http://schemas.xmlsoap.org/soap/encoding/");
            xmlWriter.WriteAttributeString("xmlns", null, null, "http://schemas.xmlsoap.org/wsdl/");*/

            definitions.Save(xmlWriter);
            xmlWriter.Flush();
            xmlWriter.Close();
        }
    }
}
