using System;
using System.IO;
using System.Xml;
using Xunit;
using SoapClientGenerator.Parser;
using SoapClientGenerator.Parser.Models;

namespace SoapClientGenerator.ParserTests;

public class WsdlParserErrorHandlingTests
{
    private readonly WsdlParser _parser = new WsdlParser();

    [Fact]
    public void Parse_EmptyString_ThrowsXmlException()
    {
        // Arrange
        var wsdlContent = string.Empty;

        // Act & Assert
        Assert.Throws<XmlException>(() => _parser.Parse(wsdlContent));
    }

    [Fact]
    public void Parse_InvalidXml_ThrowsXmlException()
    {
        // Arrange
        var wsdlContent = "<invalid>This is not valid XML";

        // Act & Assert
        Assert.Throws<XmlException>(() => _parser.Parse(wsdlContent));
    }

    [Fact]
    public void Parse_NonWsdlXml_ReturnsEmptyDefinition()
    {
        // Arrange
        var wsdlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
  <element>This is not a WSDL document</element>
</root>";

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.TargetNamespace);
        Assert.Empty(result.Types);
        Assert.Empty(result.Operations);
        Assert.Empty(result.Bindings);
        Assert.Empty(result.Services);
    }

    [Fact]
    public void Parse_WsdlWithoutTypes_ReturnsDefinitionWithoutTypes()
    {
        // Arrange
        var wsdlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
             xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/""
             xmlns:tns=""http://example.org/NoTypesService/""
             xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
             targetNamespace=""http://example.org/NoTypesService/"">
    <!-- No types section -->

    <!-- Messages -->
    <message name=""EmptyMessage"">
    </message>

    <!-- Port Type -->
    <portType name=""EmptyPortType"">
    </portType>

    <!-- Binding -->
    <binding name=""EmptyBinding"" type=""tns:EmptyPortType"">
        <soap:binding style=""document"" transport=""http://schemas.xmlsoap.org/soap/http""/>
    </binding>

    <!-- Service -->
    <service name=""EmptyService"">
        <port name=""EmptyPort"" binding=""tns:EmptyBinding"">
            <soap:address location=""http://example.org/EmptyService""/>
        </port>
    </service>
</definitions>";

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/NoTypesService/", result.TargetNamespace);
        Assert.Empty(result.Types);
        Assert.NotEmpty(result.Bindings);
        Assert.NotEmpty(result.Services);
    }

    [Fact]
    public void Parse_WsdlWithoutOperations_ReturnsDefinitionWithoutOperations()
    {
        // Arrange
        var wsdlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
             xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/""
             xmlns:tns=""http://example.org/NoOperationsService/""
             xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
             targetNamespace=""http://example.org/NoOperationsService/"">
    <!-- Types section -->
    <types>
        <xsd:schema targetNamespace=""http://example.org/NoOperationsService/"" elementFormDefault=""qualified"">
            <xsd:complexType name=""EmptyType"">
                <xsd:sequence>
                    <xsd:element name=""name"" type=""xsd:string"" minOccurs=""1"" maxOccurs=""1""/>
                </xsd:sequence>
            </xsd:complexType>
        </xsd:schema>
    </types>

    <!-- Port Type with no operations -->
    <portType name=""EmptyPortType"">
    </portType>

    <!-- Binding -->
    <binding name=""EmptyBinding"" type=""tns:EmptyPortType"">
        <soap:binding style=""document"" transport=""http://schemas.xmlsoap.org/soap/http""/>
    </binding>

    <!-- Service -->
    <service name=""EmptyService"">
        <port name=""EmptyPort"" binding=""tns:EmptyBinding"">
            <soap:address location=""http://example.org/EmptyService""/>
        </port>
    </service>
</definitions>";

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/NoOperationsService/", result.TargetNamespace);
        Assert.NotEmpty(result.Types);
        Assert.Empty(result.Operations);
        Assert.NotEmpty(result.Bindings);
        Assert.NotEmpty(result.Services);
    }

    [Fact]
    public void Parse_WsdlWithMalformedTypes_HandlesGracefully()
    {
        // Arrange
        var wsdlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
             xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/""
             xmlns:tns=""http://example.org/MalformedTypesService/""
             xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
             targetNamespace=""http://example.org/MalformedTypesService/"">
    <!-- Types section with malformed types -->
    <types>
        <xsd:schema targetNamespace=""http://example.org/MalformedTypesService/"" elementFormDefault=""qualified"">
            <!-- Missing name attribute -->
            <xsd:complexType>
                <xsd:sequence>
                    <xsd:element name=""name"" type=""xsd:string"" minOccurs=""1"" maxOccurs=""1""/>
                </xsd:sequence>
            </xsd:complexType>

            <!-- Valid type -->
            <xsd:complexType name=""ValidType"">
                <xsd:sequence>
                    <xsd:element name=""value"" type=""xsd:string"" minOccurs=""1"" maxOccurs=""1""/>
                </xsd:sequence>
            </xsd:complexType>
        </xsd:schema>
    </types>

    <!-- Port Type -->
    <portType name=""TestPortType"">
    </portType>

    <!-- Binding -->
    <binding name=""TestBinding"" type=""tns:TestPortType"">
        <soap:binding style=""document"" transport=""http://schemas.xmlsoap.org/soap/http""/>
    </binding>

    <!-- Service -->
    <service name=""TestService"">
        <port name=""TestPort"" binding=""tns:TestBinding"">
            <soap:address location=""http://example.org/TestService""/>
        </port>
    </service>
</definitions>";

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/MalformedTypesService/", result.TargetNamespace);
        Assert.NotEmpty(result.Types);

        // Should still parse the valid type
        var validType = Assert.Single(result.Types, t => t.Name == "ValidType");
        Assert.NotNull(validType);
        Assert.Equal("http://example.org/MalformedTypesService/", validType.Namespace);
    }

    [Fact]
    public void Parse_WsdlWithMalformedOperations_HandlesGracefully()
    {
        // Arrange
        var wsdlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
             xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/""
             xmlns:tns=""http://example.org/MalformedOperationsService/""
             xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
             targetNamespace=""http://example.org/MalformedOperationsService/"">

    <!-- Messages -->
    <message name=""ValidInput"">
        <part name=""parameters"" element=""tns:Request""/>
    </message>
    <message name=""ValidOutput"">
        <part name=""parameters"" element=""tns:Response""/>
    </message>

    <!-- Port Type with malformed operations -->
    <portType name=""TestPortType"">
        <!-- Missing name attribute -->
        <operation>
            <input message=""tns:ValidInput""/>
            <output message=""tns:ValidOutput""/>
        </operation>

        <!-- Missing input message -->
        <operation name=""MissingInput"">
            <output message=""tns:ValidOutput""/>
        </operation>

        <!-- Valid operation -->
        <operation name=""ValidOperation"">
            <input message=""tns:ValidInput""/>
            <output message=""tns:ValidOutput""/>
        </operation>
    </portType>

    <!-- Binding -->
    <binding name=""TestBinding"" type=""tns:TestPortType"">
        <soap:binding style=""document"" transport=""http://schemas.xmlsoap.org/soap/http""/>
        <operation name=""ValidOperation"">
            <soap:operation soapAction=""http://example.org/MalformedOperationsService/ValidOperation""/>
            <input>
                <soap:body use=""literal""/>
            </input>
            <output>
                <soap:body use=""literal""/>
            </output>
        </operation>
    </binding>

    <!-- Service -->
    <service name=""TestService"">
        <port name=""TestPort"" binding=""tns:TestBinding"">
            <soap:address location=""http://example.org/TestService""/>
        </port>
    </service>
</definitions>";

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/MalformedOperationsService/", result.TargetNamespace);

        // Should still parse the valid operation
        var validOperation = Assert.Single(result.Operations, o => o.Name == "ValidOperation");
        Assert.NotNull(validOperation);
        Assert.Equal("http://example.org/MalformedOperationsService/ValidOperation", validOperation.SoapAction);
    }

    [Fact]
    public void Parse_WsdlWithMissingNamespaces_HandlesGracefully()
    {
        // Arrange
        var wsdlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
             targetNamespace=""http://example.org/MissingNamespacesService/"">
    <!-- No namespace declarations for soap, tns, xsd -->

    <!-- Types section -->
    <types>
        <schema xmlns=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://example.org/MissingNamespacesService/"">
            <complexType name=""SimpleType"">
                <sequence>
                    <element name=""value"" type=""string"" minOccurs=""1"" maxOccurs=""1""/>
                </sequence>
            </complexType>
        </schema>
    </types>

    <!-- Service -->
    <service name=""SimpleService"">
    </service>
</definitions>";

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/MissingNamespacesService/", result.TargetNamespace);

        // Should still parse what it can
        Assert.NotEmpty(result.Namespaces);
        Assert.NotEmpty(result.Services);
    }
}
