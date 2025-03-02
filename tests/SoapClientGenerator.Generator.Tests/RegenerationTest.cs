using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using SoapClientGenerator.Generator;
using SoapClientGenerator.Parser;
using Microsoft.Extensions.Logging;

namespace SoapClientGenerator.Generator.Tests
{
    public class RegenerationTest
    {
        [Fact]
        public async Task RegenerateClient_WithNamespacePrefix_UsesCorrectPrefix()
        {
            // Arrange
            var wsdlPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ACH.wsdl");
            if (!File.Exists(wsdlPath))
            {
                // Copy the WSDL file from the parser project
                var sourceWsdlPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "SoapClientGenerator.Parser", "ACH.wsdl");
                if (!File.Exists(sourceWsdlPath))
                {
                    throw new FileNotFoundException($"WSDL file not found at {sourceWsdlPath}");
                }

                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TestData"));
                File.Copy(sourceWsdlPath, wsdlPath, true);
            }

            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "TestOutput");
            Directory.CreateDirectory(outputDir);

            var options = new SoapClientGeneratorOptions
            {
                Namespace = "ACH.Client",
                ClientName = "ACHClient",
                GenerateAsyncMethods = true,
                GenerateXmlComments = true
            };

            var generator = new SoapClientGenerator(options);

            // Act
            await generator.GenerateClientAsync(wsdlPath, outputDir);

            // Assert
            var clientFilePath = Path.Combine(outputDir, "ACHClient.cs");
            Assert.True(File.Exists(clientFilePath), "Client file should be generated");

            var clientCode = await File.ReadAllTextAsync(clientFilePath);

            // Check if the client code uses the improved serialization method
            Assert.Contains("var namespaces = new System.Xml.Serialization.XmlSerializerNamespaces();", clientCode);
            Assert.Contains("// Find the namespace prefix for the target namespace", clientCode);
            Assert.Contains("string tns = ", clientCode);
            Assert.Contains("string prefix = \"tns\"; // Default prefix if not found", clientCode);
            Assert.Contains("namespaces.Add(prefix, tns);", clientCode);
            Assert.Contains("serializer.Serialize(writer, request, namespaces);", clientCode);
            Assert.Contains("// Remove any elements with xsi:nil=\"true\" attributes", clientCode);
            Assert.Contains("element.Remove();", clientCode);
        }
    }
}
