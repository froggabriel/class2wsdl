using System;
using System.Collections.Generic;
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
        LinkedList<Type> classes = new LinkedList<Type>();
        LinkedList<Type> newClasses = new LinkedList<Type>();

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
        }

        public void Run()
        {
            _methods = _classType.GetMethods();
            Console.WriteLine("Extracted methods:");
            foreach (MethodInfo meth in _methods)
            {
                if (!(meth.Name.Equals("ToString") || meth.Name.Equals("Equals")
                    || meth.Name.Equals("GetHashCode") || meth.Name.Equals("GetType")))
                    Console.WriteLine("\t" + meth.Name);
            }

            WriteWSDL();
        }

        private void WriteWSDL()
        {
            XNamespace xmlns = "http://schemas.xmlsoap.org/wsdl/";
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
            XNamespace soap = "http://schemas.xmlsoap.org/wsdl/soap/";
            XNamespace tns = "http://tempuri.org/";

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
                if (m.Name.Equals("ToString") || m.Name.Equals("Equals")
                    || m.Name.Equals("GetHashCode") || m.Name.Equals("GetType"))
                {
                    continue;
                }

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
                                                    new XAttribute("type", GetXsdType(p.ParameterType)),
                                                    new XAttribute("nillable", !p.IsOptional)
                                                    );
                        sequence.Add(paramElement);
                    }
                    complexType.Add(sequence);
                }
                element.Add(complexType);

                schema.Add(element);

                //new classes
                AddNewClasses(schema, xsd);

                //Returns


                element = new XElement(xsd + "element", new XAttribute("name", m.Name + "Return"));
                complexType = new XElement(xsd + "complexType");
                if (m.ReturnType != typeof(void))
                {
                    sequence = new XElement(xsd + "sequence");

                    var resultElement = new XElement(xsd + "element",
                                                new XAttribute("name", m.Name + "Result"),
                                                new XAttribute("type", GetXsdType(m.ReturnType))
                                                );
                    sequence.Add(resultElement);

                    complexType.Add(sequence);
                }
                element.Add(complexType);

                schema.Add(element);

                //new classes
                AddNewClasses(schema, xsd);
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
                new XAttribute("style", "document"),
                new XAttribute("transport", "http://schemas.xmlsoap.org/soap/http")
                ));

            foreach (MethodInfo m in _methods)
            {
                var operation = new XElement(xmlns + "operation", new XAttribute("name", m.Name));
                operation.Add(new XElement(soap + "operation",
                    new XAttribute("soapAction", tns.NamespaceName + "/" + m.Name),
                    new XAttribute("style", "document"))); //MISSING SOMETHING

                var input = new XElement(xmlns + "input");
                input.Add(new XElement(soap + "body", new XAttribute("use", "literal")));
                operation.Add(input);

                var output = new XElement(xmlns + "output");
                output.Add(new XElement(soap + "body", new XAttribute("use", "literal")));
                operation.Add(output);

                binding.Add(operation);
            }

            definitions.Add(binding);

            // write service (Punto de comunicación con la clase HolaMundo)
            var service = new XElement(xmlns + "service",
                new XAttribute("name", _classType.Name),
                    new XElement(xmlns + "documentation"),
                    new XElement(xmlns + "port",
                        new XAttribute("name", _classType.Name + "Port"),
                        new XAttribute("binding", "tns:" + _classType.Name + "Binding"),
                        new XElement(soap + "address", new XAttribute("location", "http://tempuri.org/")) //default for now
                    )
                );
            definitions.Add(service);

            //Write to XML
            var settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.IndentChars = "\t";

            var xmlWriter = XmlWriter.Create(_wsdlStr, settings);
            Console.WriteLine("Writing to " + _wsdlStr + "...");
            definitions.Save(xmlWriter);
            xmlWriter.Flush();
            xmlWriter.Close();
        }

        private object GetXsdType(Type type, LinkedListNode<Type> thisNode = null)
        {

            if (type.IsPrimitive || type.Equals(typeof(String)))
            {
                if (type.Name.StartsWith("Int") || type.Name.StartsWith("Int"))
                {
                    return "xsd:integer";
                }
                else if (type.Name.Equals("Single"))
                {
                    return "xsd:byte";
                }
                return "xsd:" + type.Name.ToLower();
            }
            else if (!classes.Contains(type))
            {
                classes.AddLast(type);
                if (thisNode == null)
                    newClasses.AddLast(type);
                else
                    newClasses.AddAfter(thisNode, type);
            }

            return "tns:" + type.Name;
        }

        private void AddNewClasses(XElement schema, XNamespace xsd)
        {
            for (LinkedListNode<Type> thisNode = newClasses.First; thisNode != null; thisNode = thisNode.Next)
            {
                var sequence = new XElement(xsd + "sequence");
                var newClass = thisNode.Value;
                foreach (var p in newClass.GetProperties())
                {
                    sequence.Add(new XElement(xsd + "element",
                                        new XAttribute("name", p.Name),
                                        new XAttribute("type", GetXsdType(p.PropertyType, thisNode)),
                                        new XAttribute("nillable", p.PropertyType.IsSubclassOf(typeof(Nullable)))));
                }
                schema.Add(
                    new XElement(
                        xsd + "complexType",
                        new XAttribute("name", newClass.Name),
                         sequence)
                    );

            }
            newClasses.Clear();
        }
    }
}
