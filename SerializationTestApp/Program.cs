using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SerializationTestApp
{
    [XmlRoot(ElementName = "PostSinglePayment", Namespace = "http://www.swbc.com/")]
    public class PostSinglePayment
    {
        [XmlElement(ElementName = "Amount")]
        public double Amount { get; set; }

        [XmlElement(ElementName = "ConvenienceFee")]
        public double ConvenienceFee { get; set; }

        [XmlElement(ElementName = "TransDate")]
        public DateTime TransDate { get; set; }

        [XmlElement(ElementName = "TransType", IsNullable = true)]
        public string TransType { get; set; }

        [XmlElement(ElementName = "ABA", IsNullable = true)]
        public string ABA { get; set; }

        [XmlElement(ElementName = "AccountNumber", IsNullable = true)]
        public string AccountNumber { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SOAP Client Serialization Test");
            Console.WriteLine("==============================");

            // Create a sample request object
            var request = new PostSinglePayment
            {
                Amount = 100.0,
                ConvenienceFee = 5.0,
                TransDate = DateTime.Now,
                TransType = "Payment",
                ABA = "123456789",
                AccountNumber = null // Set to null to test nil handling
            };

            Console.WriteLine("\nOriginal Serialization Method:");
            Console.WriteLine("------------------------------");

            // Original serialization method
            var serializer = new XmlSerializer(request.GetType());
            var requestXml = new StringWriter();
            serializer.Serialize(requestXml, request);
            var requestString = requestXml.ToString();

            Console.WriteLine("Serialized XML:");
            Console.WriteLine(requestString);

            // Parse to XElement (as done in the original client)
            var requestElement = XElement.Parse(requestString);

            Console.WriteLine("\nParsed XElement:");
            Console.WriteLine(requestElement);

            // Check if namespace is preserved
            Console.WriteLine("\nNamespace Information:");
            Console.WriteLine($"Element Name: {requestElement.Name}");
            Console.WriteLine($"Element Namespace: {requestElement.Name.Namespace}");
            Console.WriteLine($"Element Namespace URI: {requestElement.Name.NamespaceName}");

            // Check child elements
            var amountElement = requestElement.Element("Amount");
            Console.WriteLine("\nChild Element (Amount):");
            Console.WriteLine($"Element Name: {amountElement?.Name}");
            Console.WriteLine($"Element Namespace: {amountElement?.Name.Namespace}");
            Console.WriteLine($"Element Namespace URI: {amountElement?.Name.NamespaceName}");

            // Create SOAP envelope with the original method
            var soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            var originalSoapEnvelope = new XDocument(
                new XElement(XName.Get("Envelope", soapNs),
                    new XAttribute(XNamespace.Xmlns + "soap", soapNs),
                    new XElement(XName.Get("Header", soapNs)),
                    new XElement(XName.Get("Body", soapNs), requestElement)
                )
            );

            Console.WriteLine("\nSOAP Envelope (Original Method):");
            Console.WriteLine(originalSoapEnvelope);

            Console.WriteLine("\nImproved Serialization Method:");
            Console.WriteLine("------------------------------");

            // Improved serialization method
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false
            };

            var ms = new MemoryStream();
            using (var writer = XmlWriter.Create(ms, settings))
            {
                serializer.Serialize(writer, request);
            }

            ms.Position = 0;
            var doc = XDocument.Load(ms);
            var improvedRequestElement = doc.Root;

            // Remove any elements with xsi:nil="true" attributes (as done in the improved client)
            var xsiNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            foreach (var element in improvedRequestElement.Descendants().ToList())
            {
                var nilAttribute = element.Attribute(xsiNamespace + "nil");
                if (nilAttribute != null && nilAttribute.Value == "true")
                {
                    Console.WriteLine($"\nRemoving nil element: {element.Name}");
                    element.Remove();
                }
            }

            Console.WriteLine("Serialized XML:");
            Console.WriteLine(improvedRequestElement);

            // Check if namespace is preserved
            Console.WriteLine("\nNamespace Information:");
            Console.WriteLine($"Element Name: {improvedRequestElement.Name}");
            Console.WriteLine($"Element Namespace: {improvedRequestElement.Name.Namespace}");
            Console.WriteLine($"Element Namespace URI: {improvedRequestElement.Name.NamespaceName}");

            // Check child elements
            var improvedAmountElement = improvedRequestElement.Element(XName.Get("Amount", "http://www.swbc.com/"));
            Console.WriteLine("\nChild Element (Amount):");
            Console.WriteLine($"Element Name: {improvedAmountElement?.Name}");
            Console.WriteLine($"Element Namespace: {improvedAmountElement?.Name.Namespace}");
            Console.WriteLine($"Element Namespace URI: {improvedAmountElement?.Name.NamespaceName}");

            // Create SOAP envelope with the improved method
            var improvedSoapEnvelope = new XDocument(
                new XElement(XName.Get("Envelope", soapNs),
                    new XAttribute(XNamespace.Xmlns + "soap", soapNs),
                    new XElement(XName.Get("Header", soapNs)),
                    new XElement(XName.Get("Body", soapNs), improvedRequestElement)
                )
            );

            Console.WriteLine("\nSOAP Envelope (Improved Method):");
            Console.WriteLine(improvedSoapEnvelope);

            Console.WriteLine("\nKey Differences:");
            Console.WriteLine("---------------");
            Console.WriteLine("1. Original method may preserve the root element namespace but loses it for child elements");
            Console.WriteLine("2. Improved method preserves namespaces for both root and child elements");
            Console.WriteLine("3. SOAP service expects elements with the correct namespace");
            Console.WriteLine("4. When accessing child elements, you need to use the correct namespace with the improved method");

            Console.WriteLine("\nHandling Null Values:");
            Console.WriteLine("-------------------");
            Console.WriteLine("1. Original serialization includes null values with xsi:nil=\"true\" attributes");
            Console.WriteLine("2. Our improved implementation removes elements with nil attributes");
            Console.WriteLine("3. This ensures that null values are not included in the SOAP request");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
