using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace SoapClientGenerator.Generator.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void SerializeRequest_WithNullValues_RemovesNilElements()
        {
            // Arrange
            var request = new TestRequest
            {
                RequiredField = "Required Value",
                OptionalField = null // This should be removed from the final XML
            };

            // Act - Original serialization method (problematic)
            var serializer = new XmlSerializer(request.GetType());
            var requestXml = new StringWriter();
            serializer.Serialize(requestXml, request);
            var originalXml = XElement.Parse(requestXml.ToString());

            // Act - Improved serialization method
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
            var improvedXml = doc.Root;

            // Remove any elements with xsi:nil="true" attributes
            var xsiNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            foreach (var element in improvedXml.Descendants().ToList())
            {
                var nilAttribute = element.Attribute(xsiNamespace + "nil");
                if (nilAttribute != null && nilAttribute.Value == "true")
                {
                    element.Remove();
                }
            }

            // Assert
            // Original XML should contain the nil element
            var xsiNil = originalXml.Descendants()
                .Where(e => e.Attribute(xsiNamespace + "nil") != null)
                .ToList();
            Assert.NotEmpty(xsiNil);
            Assert.Contains(xsiNil, e => e.Name.LocalName == "OptionalField");

            // Improved XML should not contain the nil element
            Assert.DoesNotContain(improvedXml.Descendants(), e => e.Name.LocalName == "OptionalField");
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
