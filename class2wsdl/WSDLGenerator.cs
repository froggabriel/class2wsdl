﻿using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace class2wsdl
{
    class WSDLGenerator
    {
        Assembly _assembly;
        Type _classType;
        MethodInfo[] _methods;
        readonly string _wsdlStr;
        ArrayList classes;
        ArrayList newClasses;

        public WSDLGenerator(string assemblyStr, string classStr)
        {
            try
            {
                _assembly = Assembly.Load(assemblyStr);
                Console.WriteLine("Loaded assembly: " + assemblyStr);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.StackTrace);
            }
            _classType = _assembly.GetType(classStr);
            Console.WriteLine("Got class type: " + this._classType.Name);
            _wsdlStr = classStr + ".wsdl";
            newClasses = new ArrayList();
            classes = new ArrayList();
        }

        public void Run()
        {
            _methods = _classType.GetMethods(); //TODO revisar si públicos
            Console.WriteLine("Extracted methods:");
            foreach (MethodInfo meth in _methods)
            {
                Console.WriteLine(meth.Name);
            }

            WriteWSDL();
        }

        private void WriteWSDL()
        {
            XNamespace xmlns = "http://schemas.xmlsoap.org/wsdl/";
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
            XNamespace soap = "http://schemas.xmlsoap.org/wsdl/soap/";
            XNamespace tns = "urn:" + _classType.Name;

            //write definitions
            var definitions = new XElement(
                xmlns + "definitions", //indica que definitions está dentro del namespace xmlns
                new XAttribute("name", _classType.Name),
                new XAttribute("targetNamespace", "urn:" + _classType.Name),
                new XAttribute(XNamespace.Xmlns + "wsdl", "http://schemas.xmlsoap.org/wsdl/"),
                new XAttribute(XNamespace.Xmlns + "soap", "http://schemas.xmlsoap.org/wsdl/soap/"),
                new XAttribute(XNamespace.Xmlns + "tns", "urn:" + _classType.Name),
                new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                new XAttribute(XNamespace.Xmlns + "SOAP-ENC", "http://schemas.xmlsoap.org/soap/encoding/"),
                new XAttribute("xmlns", "http://schemas.xmlsoap.org/wsdl/")
                );

            //write types (Tipos complejos)
            var types = new XElement(
                xmlns + "types",
                new XAttribute("xmlns", "http://schemas.xmlsoap.org/wsdl/")
                );
            var schema = new XElement(xsd + "schema", new XAttribute("targetNamespace", "urn:" + _classType.Name));

            // ComplexTypes

            foreach (MethodInfo m in _methods)
            {
                //Params
                var element = new XElement(xsd + "element", new XAttribute("name", m.Name));
                var complexType = new XElement(xsd + "complexType");
                XElement sequence;
                XElement paramElement;
                
                if (m.GetParameters().Length > 0)
                {
                    sequence = new XElement(xsd + "sequence");

                    foreach (var p in m.GetParameters())
                    {
                        paramElement = new XElement(xsd + "element",
                                                    new XAttribute("name", p.Name),
                                                    new XAttribute("type", GetXsdType(p.ParameterType)),//TODO
                                                    new XAttribute("nillable", !p.IsOptional)
                                                    );
                        sequence.Add(paramElement);
                    }
                    complexType.Add(sequence);
                }
                element.Add(complexType);

                schema.Add(element);

                //new classes
                foreach (Type newClass in newClasses)
                {
                    schema.Add(
                        new XElement(
                            xsd + "complexType",
                            new XAttribute("name", newClass.Name)
                            )
                        );
                }

                //Returns
                element = new XElement(xsd + "element", new XAttribute("name", m.Name + "Return"));
                complexType = new XElement(xsd + "complexType");
                sequence = new XElement(xsd + "sequence");

                var resultElement = new XElement(xsd + "element",
                                            new XAttribute("name", m.Name + "Result"),
                                            new XAttribute("type", GetXsdType(m.ReturnType))//TODO
                                            );
                sequence.Add(resultElement);

                complexType.Add(sequence);
                element.Add(complexType);

                schema.Add(element);
            }
            types.Add(schema);
            definitions.Add(types);

            //Message
            foreach (var m in _methods)
            {
                //Request
                var message = new XElement(xmlns + "message", new XAttribute("name", m.Name + "Request"));
                message.Add(new XElement(xmlns + "part", new XAttribute("name", "parameters"), new XAttribute("element", "tns:" + m.Name)));
                definitions.Add(message);

                //Response
                message = new XElement(xmlns + "message", new XAttribute("name", m.Name + "Response"));
                message.Add(new XElement(xmlns + "part", new XAttribute("name", "parameters"), new XAttribute("element", "tns:" + m.Name + "Return")));
                definitions.Add(message);
            }

            //PortType
            // write portType (Puerto para comunicar con la clase)
            var portType = new XElement(xmlns + "portType");
            foreach (MethodInfo m in _methods)
            {
                var operation = new XElement(xmlns + "operation", new XAttribute("name", m.Name));
                operation.Add(new XElement(xmlns + "input", new XAttribute("message", "tns:" + m.Name + "Request")));
                operation.Add(new XElement(xmlns + "output", new XAttribute("message", "tns:" + m.Name + "Response")));

                portType.Add(operation);
            }

            definitions.Add(portType);

            // write binding (Vinculación de los llamados con el transporte - document, SOAP over HTTP)
            var binding = new XElement(xmlns + "binding");

            binding.Add(new XElement(soap + "binding",
                new XAttribute("style","document"),
                new XAttribute("transport", "http://schemas.xmlsoap.org/soap/http")
                ));

            foreach (MethodInfo m in _methods)
            {
                var operation = new XElement(xmlns + "operation", new XAttribute("name", m.Name));
                operation.Add(new XElement(soap + "operation",
                    new XAttribute("soapAction", tns.NamespaceName + "/" + m.Name),
                    new XAttribute("style", "document"))); //MISSING SOMETHING

                var input = new XElement(xmlns +"input");
                input.Add(new XElement(soap + "body", new XAttribute("use", "literal")));
                operation.Add(input);

                var output = new XElement(xmlns + "output");
                output.Add(new XElement(soap + "body", new XAttribute("use", "literal")));
                operation.Add(output);

                binding.Add(operation);
            }

            definitions.Add(binding);

            // write service (Punto de comunicación con la clase HolaMundo)
            var service = new XElement( xmlns + "service", 
                new XAttribute("name", _classType.Name),
                    new XElement(xmlns + "documentation"),
                    new XElement(xmlns + "port",
                        new XAttribute("name", _classType.Name + "Port"),
                        new XAttribute("binding", "tns:" + _classType.Name + "Binding"),
                        new XElement(soap + "address", new XAttribute("location", "http://localhost:50503/")) //default for now
                    )
                );
            definitions.Add(service);

            //Write to XML
            var settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.IndentChars = "\t";

            var xmlWriter = XmlWriter.Create(_wsdlStr, settings);
            definitions.Save(xmlWriter);
            xmlWriter.Flush();
            xmlWriter.Close();
        }

        private object GetXsdType(Type type)
        {
            if (type.IsPrimitive || type.Equals(typeof(String))) {
                return "xsd:" + type.Name.ToLower();
            }
            else if(!classes.Contains(type))
            {
                newClasses.Add(type);
                classes.Add(type);
            }
            return "tns:"+type.Name;
        }
    }
}
