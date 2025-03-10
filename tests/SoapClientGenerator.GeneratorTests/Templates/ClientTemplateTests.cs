using System.Collections.Generic;
using SoapClientGenerator.DotnetClientGenerator;
using SoapClientGenerator.DotnetClientGenerator.Templates;
using SoapClientGenerator.Parser.Models;
using Xunit;

namespace SoapClientGenerator.GeneratorTests.Templates;

public class ClientTemplateTests
{
    private readonly SoapClientGeneratorOptions _defaultOptions;

    public ClientTemplateTests()
    {
        _defaultOptions = new SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = true,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
    }

    [Fact]
    public void GenerateClientClass_WithValidInput_GeneratesExpectedCode()
    {
        // Arrange
        var serviceName = "TestService";
        var operations = new List<WsdlOperation>
        {
            new WsdlOperation
            {
                Name = "GetData",
                Documentation = "Gets data by ID",
                SoapAction = "http://example.org/GetData",
                Input = new WsdlMessage { Name = "GetDataRequest", Element = "tns:GetDataRequest" },
                Output = new WsdlMessage { Name = "GetDataResponse", Element = "tns:GetDataResponse" }
            }
        };
        var targetNamespace = "http://example.org/";
        var namespaces = new Dictionary<string, string>
        {
            { "tns", "http://example.org/" },
            { "soap", "http://schemas.xmlsoap.org/soap/envelope/" }
        };

        // Act
        var result = ClientTemplate.GenerateClientClass(_defaultOptions, serviceName, operations, targetNamespace, namespaces);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("namespace TestNamespace", result);
        Assert.Contains("public class TestClient", result);
        Assert.Contains("public async Task<GetDataResponse> GetDataAsync", result);
        Assert.Contains("/// <summary>", result);
        Assert.Contains("using TestNamespace.DataContracts;", result);
        Assert.Contains("using TestNamespace.ServiceContracts;", result);
        Assert.Contains("_soapNs = \"http://schemas.xmlsoap.org/soap/envelope/\";", result);
        Assert.Contains("var soapAction = \"http://example.org/GetData\";", result);
    }

    [Fact]
    public void GenerateClientClass_WithoutXmlComments_DoesNotGenerateXmlComments()
    {
        // Arrange
        var options = new SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = false,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
        var serviceName = "TestService";
        var operations = new List<WsdlOperation>
        {
            new WsdlOperation
            {
                Name = "GetData",
                Documentation = "Gets data by ID",
                SoapAction = "http://example.org/GetData",
                Input = new WsdlMessage { Name = "GetDataRequest", Element = "tns:GetDataRequest" },
                Output = new WsdlMessage { Name = "GetDataResponse", Element = "tns:GetDataResponse" }
            }
        };
        var targetNamespace = "http://example.org/";
        var namespaces = new Dictionary<string, string>
        {
            { "tns", "http://example.org/" },
            { "soap", "http://schemas.xmlsoap.org/soap/envelope/" }
        };

        // Act
        var result = ClientTemplate.GenerateClientClass(options, serviceName, operations, targetNamespace, namespaces);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("/// <summary>", result);
    }

    [Fact]
    public void GenerateClientClass_WithSyncMethods_GeneratesSyncMethods()
    {
        // Arrange
        var options = new SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = false,
            GenerateXmlComments = true,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
        var serviceName = "TestService";
        var operations = new List<WsdlOperation>
        {
            new WsdlOperation
            {
                Name = "GetData",
                Documentation = "Gets data by ID",
                SoapAction = "http://example.org/GetData",
                Input = new WsdlMessage { Name = "GetDataRequest", Element = "tns:GetDataRequest" },
                Output = new WsdlMessage { Name = "GetDataResponse", Element = "tns:GetDataResponse" }
            }
        };
        var targetNamespace = "http://example.org/";
        var namespaces = new Dictionary<string, string>
        {
            { "tns", "http://example.org/" },
            { "soap", "http://schemas.xmlsoap.org/soap/envelope/" }
        };

        // Act
        var result = ClientTemplate.GenerateClientClass(options, serviceName, operations, targetNamespace, namespaces);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("public GetDataResponse GetData", result);
        Assert.DoesNotContain("public async Task<GetDataResponse> GetDataAsync", result);
    }

    [Fact]
    public void GenerateClientClass_WithAuthHeader_GeneratesAuthHeaderCode()
    {
        // Arrange
        var serviceName = "TestService";
        var operations = new List<WsdlOperation>
        {
            new WsdlOperation
            {
                Name = "GetData",
                Documentation = "Gets data by ID",
                SoapAction = "http://example.org/GetData",
                Input = new WsdlMessage { Name = "GetDataRequest", Element = "tns:GetDataRequest" },
                Output = new WsdlMessage { Name = "GetDataResponse", Element = "tns:GetDataResponse" },
                AuthHeader = new WsdlAuthHeader
                {
                    Name = "AuthHeader",
                    Element = "tns:SWBCAuthHeader",
                    TypeName = "SWBCAuthHeader"
                }
            }
        };
        var targetNamespace = "http://example.org/";
        var namespaces = new Dictionary<string, string>
        {
            { "tns", "http://example.org/" },
            { "soap", "http://schemas.xmlsoap.org/soap/envelope/" }
        };

        // Act
        var result = ClientTemplate.GenerateClientClass(_defaultOptions, serviceName, operations, targetNamespace, namespaces);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("SetAuthCredentials", result);
        Assert.Contains("_useAuthHeader = true;", result);
        Assert.Contains("var soapEnvelope = CreateSoapEnvelope(soapAction, requestElement, true);", result);
    }

    [Fact]
    public void GenerateDataContract_WithValidInput_GeneratesExpectedCode()
    {
        // Arrange
        var typeName = "TestType";
        var properties = new Dictionary<string, (string Type, bool IsRequired)>
        {
            { "id", ("int", true) },
            { "name", ("string", true) },
            { "description", ("string", false) },
            { "items", ("List<string>", false) }
        };

        // Act
        var result = ClientTemplate.GenerateDataContract(_defaultOptions, typeName, properties);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("namespace TestNamespace.DataContracts", result);
        Assert.Contains("public class TestType", result);
        Assert.Contains("public int id { get; set; }", result);
        Assert.Contains("public string name { get; set; }", result);
        Assert.Contains("public string? description { get; set; }", result);
        Assert.Contains("public List<string> items { get; set; }", result);
        Assert.Contains("[XmlElement(ElementName = \"id\")]", result);
        Assert.Contains("[XmlElement(ElementName = \"description\", IsNullable = true)]", result);
    }

    [Fact]
    public void GenerateDataContract_WithCSharpKeywords_EscapesKeywords()
    {
        // Arrange
        var typeName = "TestType";
        var properties = new Dictionary<string, (string Type, bool IsRequired)>
        {
            { "class", ("string", true) },
            { "int", ("int", true) },
            { "string", ("string", false) }
        };

        // Act
        var result = ClientTemplate.GenerateDataContract(_defaultOptions, typeName, properties);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("public string @class { get; set; }", result);
        Assert.Contains("public int @int { get; set; }", result);
        Assert.Contains("public string? @string { get; set; }", result);
    }

    [Fact]
    public void GenerateEnum_WithValidInput_GeneratesExpectedCode()
    {
        // Arrange
        var enumName = "TestEnum";
        var values = new List<string> { "Value1", "Value2", "Value3" };

        // Act
        var result = ClientTemplate.GenerateEnum(_defaultOptions, enumName, values);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("namespace TestNamespace.DataContracts", result);
        Assert.Contains("public enum TestEnum", result);
        Assert.Contains("[XmlEnum(Name = \"Value1\")]", result);
        Assert.Contains("Value1", result);
        Assert.Contains("Value2", result);
        Assert.Contains("Value3", result);
    }

    [Fact]
    public void GenerateEnum_WithoutXmlComments_DoesNotGenerateXmlComments()
    {
        // Arrange
        var options = new SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = false,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
        var enumName = "TestEnum";
        var values = new List<string> { "Value1", "Value2", "Value3" };

        // Act
        var result = ClientTemplate.GenerateEnum(options, enumName, values);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("/// <summary>", result);
    }
}
