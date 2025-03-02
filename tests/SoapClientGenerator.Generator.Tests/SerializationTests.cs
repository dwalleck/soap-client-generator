using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace SoapClientGenerator.Generator.Tests
{
    public class SerializationTests
    {
        private readonly ITestOutputHelper _output;

        public SerializationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestRequestSerialization_CurrentImplementation()
        {
            // Create a sample request object
            var request = new SampleRequest
            {
                Amount = 100.0,
                ConvenienceFee = 5.0,
                TransDate = DateTime.Now,
                TransType = "Payment",
                ABA = "123456789",
                AccountNumber = "987654321",
                IndividualName = "John Doe"
            };

            // Current implementation
            var serializer = new XmlSerializer(request.GetType());
            var requestXml = new StringWriter();
            serializer.Serialize(requestXml, request);
            var requestString = requestXml.ToString();

            // Output the serialized XML for inspection
            _output.WriteLine("Serialized XML (Current Implementation):");
            _output.WriteLine(requestString);

            // Parse to XElement (as done in the client)
            var requestElement = XElement.Parse(requestString);

            // Output the parsed XElement for inspection
            _output.WriteLine("\nParsed XElement (Current Implementation):");
            _output.WriteLine(requestElement.ToString());

            // Check if the namespace is preserved
            Assert.Contains("xmlns", requestString);

            // The namespace might be lost when parsing to XElement
            // This is likely the issue with the current implementation
        }

        [Fact]
        public void TestRequestSerialization_ImprovedImplementation()
        {
            // Create a sample request object
            var request = new SampleRequest
            {
                Amount = 100.0,
                ConvenienceFee = 5.0,
                TransDate = DateTime.Now,
                TransType = "Payment",
                ABA = "123456789",
                AccountNumber = "987654321",
                IndividualName = "John Doe"
            };

            // Improved implementation that preserves namespaces
            var serializer = new XmlSerializer(request.GetType());
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
            var requestElement = doc.Root;

            // Output the serialized XML for inspection
            _output.WriteLine("Serialized XML (Improved Implementation):");
            _output.WriteLine(requestElement.ToString());

            // Check if the namespace is preserved
            Assert.NotNull(requestElement.Name.Namespace);
            Assert.NotEqual("", requestElement.Name.Namespace.NamespaceName);

            // Create a SOAP envelope with this element
            var soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            var soapEnvelope = new XDocument(
                new XElement(XName.Get("Envelope", soapNs),
                    new XAttribute(XNamespace.Xmlns + "soap", soapNs),
                    new XElement(XName.Get("Header", soapNs)),
                    new XElement(XName.Get("Body", soapNs), requestElement)
                )
            );

            // Output the SOAP envelope for inspection
            _output.WriteLine("\nSOAP Envelope (Improved Implementation):");
            _output.WriteLine(soapEnvelope.ToString());

            // Verify the SOAP envelope structure
            Assert.NotNull(soapEnvelope.Root);
            Assert.Equal(soapNs, soapEnvelope.Root.Name.Namespace.NamespaceName);
            Assert.Equal("Envelope", soapEnvelope.Root.Name.LocalName);

            var bodyElement = soapEnvelope.Root.Element(XName.Get("Body", soapNs));
            Assert.NotNull(bodyElement);

            var requestInBody = bodyElement.Elements().FirstOrDefault();
            Assert.NotNull(requestInBody);
            Assert.Equal(requestElement.Name, requestInBody.Name);
        }
    }

    // Sample request class for testing
    [XmlRoot(ElementName = "PostSinglePayment", Namespace = "http://www.swbc.com/")]
    public class SampleRequest
    {
        [XmlElement(ElementName = "Amount")]
        public double Amount { get; set; }

        [XmlElement(ElementName = "ConvenienceFee")]
        public double ConvenienceFee { get; set; }

        [XmlElement(ElementName = "TransDate")]
        public DateTime TransDate { get; set; }

        [XmlElement(ElementName = "TransType")]
        public string TransType { get; set; }

        [XmlElement(ElementName = "ABA")]
        public string ABA { get; set; }

        [XmlElement(ElementName = "AccountNumber")]
        public string AccountNumber { get; set; }

        [XmlElement(ElementName = "IndividualName")]
        public string IndividualName { get; set; }
    }
}
