using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace SoapClientGenerator.Generator.Tests
{
    public class NamespaceSerializationTests
    {
        [Fact]
        public void SerializeRequest_WithNamespacePrefix_UsesCorrectPrefix()
        {
            // Arrange
            var request = new TestRequest
            {
                RequiredField = "Required Value",
                OptionalField = null // This should be removed from the final XML
            };

            // Act - Serialize with namespace prefix
            var serializer = new XmlSerializer(request.GetType());
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("swbc", "http://test.com/");

            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false
            };

            var ms = new MemoryStream();
            using (var writer = XmlWriter.Create(ms, settings))
            {
                serializer.Serialize(writer, request, namespaces);
            }

            ms.Position = 0;
            var doc = XDocument.Load(ms);
            var requestElement = doc.Root;

            // Remove any elements with xsi:nil="true" attributes
            var xsiNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            foreach (var element in requestElement.Descendants().ToList())
            {
                var nilAttribute = element.Attribute(xsiNamespace + "nil");
                if (nilAttribute != null && nilAttribute.Value == "true")
                {
                    element.Remove();
                }
            }

            // Convert to string for easier assertion
            var resultXml = requestElement.ToString();

            // Assert
            Assert.Contains("swbc:TestRequest", resultXml);
            Assert.Contains("swbc:RequiredField", resultXml);
            Assert.DoesNotContain("nil=\"true\"", resultXml);
            Assert.DoesNotContain("OptionalField", resultXml);
        }

        [XmlRoot(ElementName = "TestRequest", Namespace = "http://test.com/")]
        public class TestRequest
        {
            [XmlElement(ElementName = "RequiredField")]
            public string RequiredField { get; set; }

            [XmlElement(ElementName = "OptionalField", IsNullable = true)]
            public string OptionalField { get; set; }
        }
    }
}
