using System.IO;
using System.Linq;
using SoapClientGenerator.Parser;
using SoapClientGenerator.Parser.Models;
using Xunit;

namespace SoapClientGenerator.ParserTests;

public class WsdlParserIntegrationTests
{
    private readonly WsdlParser _parser = new WsdlParser();
    private readonly string _simpleWsdlPath = Path.Combine("TestResources", "SimpleService.wsdl");
    private readonly string _complexWsdlPath = Path.Combine("TestResources", "ComplexService.wsdl");
    private readonly string _overloadedWsdlPath = Path.Combine("TestResources", "OverloadedService.wsdl");

    [Fact]
    public void SimpleService_ParsesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_simpleWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        // Check basic structure
        Assert.NotNull(result);
        Assert.Equal("http://example.org/SimpleService/", result.TargetNamespace);

        // Check namespaces
        Assert.Contains(result.Namespaces, ns => ns.Key == "tns" && ns.Value == "http://example.org/SimpleService/");

        // Check types
        Assert.Equal(5, result.Types.Count); // 3 complex types (GetDataRequest, GetDataResponse, DataType) + 1 enum + 1 array

        // Check operations
        Assert.Single(result.Operations);
        var operation = result.Operations.First();
        Assert.Equal("GetData", operation.Name);
        Assert.Equal("http://example.org/SimpleService/GetData", operation.SoapAction);

        // Check bindings
        Assert.Single(result.Bindings);
        var binding = result.Bindings.First();
        Assert.Equal("SimpleServiceSoapBinding", binding.Name);
        Assert.Equal("tns:SimpleServicePortType", binding.Type);

        // Check services
        Assert.Single(result.Services);
        var service = result.Services.First();
        Assert.Equal("SimpleService", service.Name);
        Assert.Single(service.Ports);
        Assert.Equal("http://example.org/SimpleService", service.Ports.First().Location);
    }

    [Fact]
    public void ComplexService_ParsesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        // Check basic structure
        Assert.NotNull(result);
        Assert.Equal("http://example.org/ComplexService/", result.TargetNamespace);

        // Check types
        var types = result.Types.ToList();
        Assert.Contains(types, t => t.Name == "UserType" && t.Kind == WsdlTypeKind.Complex);
        Assert.Contains(types, t => t.Name == "AddressType" && t.Kind == WsdlTypeKind.Complex);
        Assert.Contains(types, t => t.Name == "StatusEnum" && t.Kind == WsdlTypeKind.Enum);
        Assert.Contains(types, t => t.Name == "RoleEnum" && t.Kind == WsdlTypeKind.Enum);
        Assert.Contains(types, t => t.Name == "UserArray" && t.IsArrayType);

        // Check operations
        Assert.Equal(4, result.Operations.Count);
        var operations = result.Operations.ToList();
        Assert.Contains(operations, o => o.Name == "GetUser");
        Assert.Contains(operations, o => o.Name == "CreateUser");
        Assert.Contains(operations, o => o.Name == "UpdateUser");
        Assert.Contains(operations, o => o.Name == "DeleteUser");

        // Check auth headers
        var getUser = operations.First(o => o.Name == "GetUser");
        Assert.NotNull(getUser.AuthHeader);
        Assert.Equal("AuthHeader", getUser.AuthHeader.Name);
        Assert.Contains("SWBCAuthHeader", getUser.AuthHeader.Element);

        // Check service
        Assert.Single(result.Services);
        var service = result.Services.First();
        Assert.Equal("UserService", service.Name);
        Assert.Equal("http://example.org/UserService", service.Ports.First().Location);
    }

    [Fact]
    public void OverloadedService_ParsesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_overloadedWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        // Check basic structure
        Assert.NotNull(result);
        Assert.Equal("http://example.org/OverloadedService/", result.TargetNamespace);

        // Check types
        var types = result.Types.ToList();
        Assert.Contains(types, t => t.Name == "ProductType" && t.Kind == WsdlTypeKind.Complex);
        Assert.Contains(types, t => t.Name == "CategoryEnum" && t.Kind == WsdlTypeKind.Enum);

        // Check operations - should have 3 operations with unique names based on input names
        Assert.Equal(3, result.Operations.Count);
        var operations = result.Operations.ToList();
        Assert.Contains(operations, o => o.Name == "GetProductById");
        Assert.Contains(operations, o => o.Name == "GetProductByCode");
        Assert.Contains(operations, o => o.Name == "GetProductByName");

        // Check SOAP actions
        var byId = operations.First(o => o.Name == "GetProductById");
        Assert.Equal("http://example.org/OverloadedService/GetProductById", byId.SoapAction);

        var byCode = operations.First(o => o.Name == "GetProductByCode");
        Assert.Equal("http://example.org/OverloadedService/GetProductByCode", byCode.SoapAction);

        var byName = operations.First(o => o.Name == "GetProductByName");
        Assert.Equal("http://example.org/OverloadedService/GetProductByName", byName.SoapAction);

        // Check service
        Assert.Single(result.Services);
        var service = result.Services.First();
        Assert.Equal("ProductService", service.Name);
        Assert.Equal("http://example.org/ProductService", service.Ports.First().Location);
    }

    [Fact]
    public void ComplexService_TypeProperties_ParsedCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);
        var userType = result.Types.First(t => t.Name == "UserType");

        // Assert
        Assert.Equal(8, userType.Properties.Count);

        // Check required vs optional properties
        var username = userType.Properties.First(p => p.Name == "username");
        Assert.True(username.IsRequired);

        var firstName = userType.Properties.First(p => p.Name == "firstName");
        Assert.False(firstName.IsRequired);

        // Check collection property
        var roles = userType.Properties.First(p => p.Name == "roles");
        Assert.True(roles.IsCollection);

        // Check nested complex type property
        var address = userType.Properties.First(p => p.Name == "address");
        Assert.Equal("tns:AddressType", address.Type);
    }

    [Fact]
    public void SimpleService_ArrayType_ParsedCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_simpleWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);
        var dataArray = result.Types.First(t => t.Name == "DataArray");

        // Assert
        Assert.True(dataArray.IsArrayType);
        Assert.Equal("tns:DataType", dataArray.ArrayItemType);
        Assert.Single(dataArray.Properties);

        var itemProperty = dataArray.Properties.First();
        Assert.Equal("item", itemProperty.Name);
        Assert.Equal("tns:DataType", itemProperty.Type);
        Assert.True(itemProperty.IsCollection);
    }

    [Fact]
    public void ComplexService_EnumTypes_ParsedCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);
        var statusEnum = result.Types.First(t => t.Name == "StatusEnum");
        var roleEnum = result.Types.First(t => t.Name == "RoleEnum");

        // Assert
        Assert.True(statusEnum.IsEnum);
        Assert.Equal(WsdlTypeKind.Enum, statusEnum.Kind);
        Assert.Equal(4, statusEnum.EnumValues.Count);
        Assert.Contains("Active", statusEnum.EnumValues);
        Assert.Contains("Inactive", statusEnum.EnumValues);
        Assert.Contains("Suspended", statusEnum.EnumValues);
        Assert.Contains("Pending", statusEnum.EnumValues);

        Assert.True(roleEnum.IsEnum);
        Assert.Equal(WsdlTypeKind.Enum, roleEnum.Kind);
        Assert.Equal(4, roleEnum.EnumValues.Count);
        Assert.Contains("Admin", roleEnum.EnumValues);
        Assert.Contains("User", roleEnum.EnumValues);
        Assert.Contains("Guest", roleEnum.EnumValues);
        Assert.Contains("Moderator", roleEnum.EnumValues);
    }

    [Fact]
    public void ComplexService_AuthHeader_ParsedCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);
        var operations = result.Operations.ToList();

        // Assert
        // All operations should have auth headers
        foreach (var operation in operations)
        {
            Assert.NotNull(operation.AuthHeader);
            Assert.Equal("AuthHeader", operation.AuthHeader.Name);
            Assert.Contains("SWBCAuthHeader", operation.AuthHeader.Element);
            Assert.Equal("SWBCAuthHeader", operation.AuthHeader.TypeName);
        }

        // Check the auth header type itself
        var authHeaderType = result.Types.FirstOrDefault(t => t.Name == "SWBCAuthHeader");
        Assert.NotNull(authHeaderType);
        Assert.Equal(3, authHeaderType.Properties.Count);
        Assert.Contains(authHeaderType.Properties, p => p.Name == "username" && p.IsRequired);
        Assert.Contains(authHeaderType.Properties, p => p.Name == "password" && p.IsRequired);
        Assert.Contains(authHeaderType.Properties, p => p.Name == "apiKey" && !p.IsRequired);
    }
}
