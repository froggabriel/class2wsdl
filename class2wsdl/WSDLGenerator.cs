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
            }
            catch (FileNotFoundException e)
            {
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
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.IndentChars = "\t";
            XmlWriter xmlWriter = XmlTextWriter.Create(this._wsdlStr, settings);

            XNamespace xmlns = "http://schemas.xmlsoap.org/wsdl/";
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
            XNamespace soap = "http://schemas.xmlsoap.org/wsdl/soap/";
            XNamespace tns = "urn:" + this._classType.Name;
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
            XElement schema = new XElement(xsd + "schema", new XAttribute("targetNamespace", "urn:" + this._classType.Name));

            // ComplexTypes
            foreach (MethodInfo m in _methods)
            {
                //Params
                XElement element = new XElement(xsd + "element", new XAttribute("name", m.Name));
                XElement complexType = new XElement(xsd + "complexType");
                XElement sequence;
                if (m.GetParameters().Length > 0)
                {
                    sequence = new XElement(xsd + "sequence");

                    foreach (ParameterInfo p in m.GetParameters())
                    {
                        XElement paramElement = new XElement(xsd + "element",
                                                    new XAttribute("name", p.Name),
                                                    new XAttribute("type", this.GetXsdType(p.GetType())),//TODO
                                                    new XAttribute("nillable", !p.IsOptional)
                                                    );
                        sequence.Add(paramElement);
                    }
                    complexType.Add(sequence);
                }
                element.Add(complexType);

                schema.Add(element);

                //Returns
                element = new XElement(xsd + "element", new XAttribute("name", m.Name + "Return"));
                complexType = new XElement(xsd + "complexType");
                sequence = new XElement(xsd + "sequence");

                XElement resultElement = new XElement(xsd + "element",
                                            new XAttribute("name", m.Name + "Result"),
                                            new XAttribute("type", this.GetXsdType(m.ReturnType))//TODO
                                            );
                sequence.Add(resultElement);

                complexType.Add(sequence);
                element.Add(complexType);

                schema.Add(element);
            }
            types.Add(schema);
            definitions.Add(types);

            //Message
            foreach (MethodInfo m in _methods)
            {
                XElement element = new XElement(xmlns + "message", new XAttribute("name", m.Name + "Request"));
                element.Add(new XElement("part", new XAttribute("name", "parameters"), new XAttribute("element", tns + m.Name)));
                definitions.Add(element);

                element = new XElement(xmlns + "message", new XAttribute("name", m.Name + "Response"));
                element.Add(new XElement("part", new XAttribute("name", "parameters"), new XAttribute("element", tns + m.Name + "Return")));
                definitions.Add(element);
            }

            //PortType
            // write portType (Puerto para comunicar con la clase)
            portType = new XElement( xmlns + "portType" );
            foreach (MethodInfo m in _methods)
            {
                XElement element = new XElement("operation", new XAttribute("name", m.Name));
                element.Add(new XElement("input", new XAttribute("message", tns + m.Name + "Request")));
                element.Add(new XElement("output", new XAttribute("message", tns + m.Name + "Response")));

                portType.Add(element);
            }

            definitions.Add(portType);

            // write binding (Vinculación de los llamados con el transporte - document, SOAP over HTTP)
            binding = new XElement(  xmlns + "binding" );

            binding.Add(new XElement(soap + "binding",
                new XAttribute("style","document"),
                new XAttribute("transport", "http://schemas.xmlsoap.org/soap/http")
                ));

            foreach (MethodInfo m in _methods)
            {
                XElement element = new XElement("operation", new XAttribute("name", m.Name));
                //element.Add(new XElement(soap+"operation", new XAttribute("soapAction", tns +"#"+ m.Name))); //MISSING SOMETHING

                XElement input = new XElement("input");
                input.Add(new XElement(soap + "body", new XAttribute("use", "literal")));
                element.Add(input);

                XElement output = new XElement("output");
                input.Add(new XElement(soap + "body", new XAttribute("use", "literal")));
                element.Add(output);

                binding.Add(element);
            }

            definitions.Add(portType);

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

        private object GetXsdType(Type type)
        {
            //TODO
            return "xsd:string";
        }
    }
}
