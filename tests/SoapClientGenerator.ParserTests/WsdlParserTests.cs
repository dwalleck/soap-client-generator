using System.IO;
using System.Linq;
using System.Xml.Linq;
using SoapClientGenerator.Parser;
using SoapClientGenerator.Parser.Models;
using Xunit;

namespace SoapClientGenerator.ParserTests;

public class WsdlParserTests
{
    private readonly WsdlParser _parser = new WsdlParser();
    private readonly string _simpleWsdlPath = Path.Combine("TestResources", "SimpleService.wsdl");
    private readonly string _complexWsdlPath = Path.Combine("TestResources", "ComplexService.wsdl");
    private readonly string _overloadedWsdlPath = Path.Combine("TestResources", "OverloadedService.wsdl");

    #region Parse Method Tests

    [Fact]
    public void Parse_SimpleWsdl_ReturnsCorrectDefinition()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_simpleWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/SimpleService/", result.TargetNamespace);
        Assert.NotEmpty(result.Namespaces);
        Assert.NotEmpty(result.Types);
        Assert.NotEmpty(result.Operations);
        Assert.NotEmpty(result.Bindings);
        Assert.NotEmpty(result.Services);
    }

    [Fact]
    public void Parse_ComplexWsdl_ReturnsCorrectDefinition()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/ComplexService/", result.TargetNamespace);
        Assert.NotEmpty(result.Namespaces);
        Assert.NotEmpty(result.Types);
        Assert.NotEmpty(result.Operations);
        Assert.NotEmpty(result.Bindings);
        Assert.NotEmpty(result.Services);
    }

    [Fact]
    public void Parse_OverloadedWsdl_ReturnsCorrectDefinition()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_overloadedWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/OverloadedService/", result.TargetNamespace);
        Assert.NotEmpty(result.Namespaces);
        Assert.NotEmpty(result.Types);
        Assert.NotEmpty(result.Operations);
        Assert.NotEmpty(result.Bindings);
        Assert.NotEmpty(result.Services);
    }

    #endregion

    #region Namespaces Tests

    [Fact]
    public void Parse_SimpleWsdl_ParsesNamespacesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_simpleWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result.Namespaces);
        Assert.True(result.Namespaces.Count >= 4); // At least 4 namespaces in the simple WSDL
        Assert.Contains(result.Namespaces, ns => ns.Key == "soap" && ns.Value == "http://schemas.xmlsoap.org/wsdl/soap/");
        Assert.Contains(result.Namespaces, ns => ns.Key == "tns" && ns.Value == "http://example.org/SimpleService/");
        Assert.Contains(result.Namespaces, ns => ns.Key == "xsd" && ns.Value == "http://www.w3.org/2001/XMLSchema");
    }

    [Fact]
    public void Parse_ComplexWsdl_ParsesNamespacesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result.Namespaces);
        Assert.True(result.Namespaces.Count >= 4); // At least 4 namespaces in the complex WSDL
        Assert.Contains(result.Namespaces, ns => ns.Key == "soap" && ns.Value == "http://schemas.xmlsoap.org/wsdl/soap/");
        Assert.Contains(result.Namespaces, ns => ns.Key == "tns" && ns.Value == "http://example.org/ComplexService/");
        Assert.Contains(result.Namespaces, ns => ns.Key == "xsd" && ns.Value == "http://www.w3.org/2001/XMLSchema");
    }

    #endregion

    #region Types Tests

    [Fact]
    public void Parse_SimpleWsdl_ParsesTypesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_simpleWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Types);

        // Check for complex type
        var dataType = result.Types.FirstOrDefault(t => t.Name == "DataType");
        Assert.NotNull(dataType);
        Assert.Equal(WsdlTypeKind.Complex, dataType.Kind);
        Assert.Equal("http://example.org/SimpleService/", dataType.Namespace);
        Assert.True(dataType.ElementFormQualified);
        Assert.Equal(3, dataType.Properties.Count);

        // Check for enum type
        var statusEnum = result.Types.FirstOrDefault(t => t.Name == "StatusEnum");
        Assert.NotNull(statusEnum);
        Assert.Equal(WsdlTypeKind.Enum, statusEnum.Kind);
        Assert.True(statusEnum.IsEnum);
        Assert.Equal(3, statusEnum.EnumValues.Count);
        Assert.Contains("Active", statusEnum.EnumValues);
        Assert.Contains("Inactive", statusEnum.EnumValues);
        Assert.Contains("Pending", statusEnum.EnumValues);

        // Check for array type
        var dataArray = result.Types.FirstOrDefault(t => t.Name == "DataArray");
        Assert.NotNull(dataArray);
        Assert.Equal(WsdlTypeKind.Complex, dataArray.Kind);
        Assert.True(dataArray.IsArrayType);
        Assert.Contains("tns:DataType", dataArray.ArrayItemType);
    }

    [Fact]
    public void Parse_ComplexWsdl_ParsesTypesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Types);

        // Check for complex type
        var userType = result.Types.FirstOrDefault(t => t.Name == "UserType");
        Assert.NotNull(userType);
        Assert.Equal(WsdlTypeKind.Complex, userType.Kind);
        Assert.Equal("http://example.org/ComplexService/", userType.Namespace);
        Assert.True(userType.ElementFormQualified);
        Assert.Equal(8, userType.Properties.Count);

        // Check for nested complex type
        var addressType = result.Types.FirstOrDefault(t => t.Name == "AddressType");
        Assert.NotNull(addressType);
        Assert.Equal(WsdlTypeKind.Complex, addressType.Kind);
        Assert.Equal(5, addressType.Properties.Count);

        // Check for enum types
        var statusEnum = result.Types.FirstOrDefault(t => t.Name == "StatusEnum");
        Assert.NotNull(statusEnum);
        Assert.Equal(WsdlTypeKind.Enum, statusEnum.Kind);
        Assert.True(statusEnum.IsEnum);
        Assert.Equal(4, statusEnum.EnumValues.Count);

        var roleEnum = result.Types.FirstOrDefault(t => t.Name == "RoleEnum");
        Assert.NotNull(roleEnum);
        Assert.Equal(WsdlTypeKind.Enum, roleEnum.Kind);
        Assert.True(roleEnum.IsEnum);
        Assert.Equal(4, roleEnum.EnumValues.Count);

        // Check for array type
        var userArray = result.Types.FirstOrDefault(t => t.Name == "UserArray");
        Assert.NotNull(userArray);
        Assert.Equal(WsdlTypeKind.Complex, userArray.Kind);
        Assert.True(userArray.IsArrayType);
        Assert.Contains("tns:UserType", userArray.ArrayItemType);
    }

    [Fact]
    public void Parse_ComplexType_ParsesPropertiesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);
        var userType = result.Types.FirstOrDefault(t => t.Name == "UserType");

        // Assert
        Assert.NotNull(userType);
        Assert.Equal(8, userType.Properties.Count);

        // Required property
        var username = userType.Properties.FirstOrDefault(p => p.Name == "username");
        Assert.NotNull(username);
        Assert.True(username.IsRequired);
        Assert.False(username.IsCollection);

        // Optional property
        var firstName = userType.Properties.FirstOrDefault(p => p.Name == "firstName");
        Assert.NotNull(firstName);
        Assert.False(firstName.IsRequired);
        Assert.False(firstName.IsCollection);

        // Collection property
        var roles = userType.Properties.FirstOrDefault(p => p.Name == "roles");
        Assert.NotNull(roles);
        Assert.False(roles.IsRequired);
        Assert.True(roles.IsCollection);
    }

    #endregion

    #region Operations Tests

    [Fact]
    public void Parse_SimpleWsdl_ParsesOperationsCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_simpleWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Operations);
        Assert.Single(result.Operations);

        var operation = result.Operations.First();
        Assert.Equal("GetData", operation.Name);
        Assert.Equal("Retrieves data for the specified ID", operation.Documentation);
        Assert.Equal("http://example.org/SimpleService/GetData", operation.SoapAction);

        Assert.NotNull(operation.Input);
        Assert.Equal("GetDataRequest", operation.Input.Name);

        Assert.NotNull(operation.Output);
        Assert.Equal("GetDataResponse", operation.Output.Name);
    }

    [Fact]
    public void Parse_ComplexWsdl_ParsesOperationsCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Operations);
        Assert.Equal(4, result.Operations.Count);

        // Check GetUser operation
        var getUser = result.Operations.FirstOrDefault(o => o.Name == "GetUser");
        Assert.NotNull(getUser);
        Assert.Equal("Retrieves user information by ID", getUser.Documentation);
        Assert.Equal("http://example.org/ComplexService/GetUser", getUser.SoapAction);

        // Check auth header
        Assert.NotNull(getUser.AuthHeader);
        Assert.Equal("AuthHeader", getUser.AuthHeader.Name);
        Assert.Contains("SWBCAuthHeader", getUser.AuthHeader.Element);
    }

    [Fact]
    public void Parse_OverloadedWsdl_ParsesOverloadedOperationsCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_overloadedWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Operations);
        Assert.Equal(3, result.Operations.Count);

        // Check that all operations have unique names based on input name
        var operations = result.Operations.ToList();
        Assert.Contains(operations, o => o.Name == "GetProductById");
        Assert.Contains(operations, o => o.Name == "GetProductByCode");
        Assert.Contains(operations, o => o.Name == "GetProductByName");

        // Check that each operation has the correct SOAP action
        var byId = operations.First(o => o.Name == "GetProductById");
        Assert.Equal("http://example.org/OverloadedService/GetProductById", byId.SoapAction);

        var byCode = operations.First(o => o.Name == "GetProductByCode");
        Assert.Equal("http://example.org/OverloadedService/GetProductByCode", byCode.SoapAction);

        var byName = operations.First(o => o.Name == "GetProductByName");
        Assert.Equal("http://example.org/OverloadedService/GetProductByName", byName.SoapAction);
    }

    #endregion

    #region Bindings Tests

    [Fact]
    public void Parse_SimpleWsdl_ParsesBindingsCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_simpleWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Bindings);
        Assert.Single(result.Bindings);

        var binding = result.Bindings.First();
        Assert.Equal("SimpleServiceSoapBinding", binding.Name);
        Assert.Equal("tns:SimpleServicePortType", binding.Type);
        Assert.Equal("http://schemas.xmlsoap.org/soap/http", binding.Transport);

        Assert.NotEmpty(binding.OperationSoapActions);
        Assert.Single(binding.OperationSoapActions);
        Assert.Contains(binding.OperationSoapActions, kv =>
            kv.Key == "GetData" && kv.Value == "http://example.org/SimpleService/GetData");
    }

    [Fact]
    public void Parse_ComplexWsdl_ParsesBindingsCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Bindings);
        Assert.Single(result.Bindings);

        var binding = result.Bindings.First();
        Assert.Equal("UserServiceSoapBinding", binding.Name);
        Assert.Equal("tns:UserServicePortType", binding.Type);
        Assert.Equal("http://schemas.xmlsoap.org/soap/http", binding.Transport);

        Assert.NotEmpty(binding.OperationSoapActions);
        Assert.Equal(4, binding.OperationSoapActions.Count);

        Assert.Contains(binding.OperationSoapActions, kv =>
            kv.Key == "GetUser" && kv.Value == "http://example.org/ComplexService/GetUser");
        Assert.Contains(binding.OperationSoapActions, kv =>
            kv.Key == "CreateUser" && kv.Value == "http://example.org/ComplexService/CreateUser");
        Assert.Contains(binding.OperationSoapActions, kv =>
            kv.Key == "UpdateUser" && kv.Value == "http://example.org/ComplexService/UpdateUser");
        Assert.Contains(binding.OperationSoapActions, kv =>
            kv.Key == "DeleteUser" && kv.Value == "http://example.org/ComplexService/DeleteUser");
    }

    [Fact]
    public void Parse_OverloadedWsdl_ParsesOverloadedBindingsCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_overloadedWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Bindings);
        Assert.Single(result.Bindings);

        var binding = result.Bindings.First();
        Assert.Equal("ProductServiceSoapBinding", binding.Name);

        Assert.NotEmpty(binding.OperationSoapActions);
        Assert.Equal(3, binding.OperationSoapActions.Count);

        // Check that overloaded operations are correctly mapped by input name
        Assert.Contains(binding.OperationSoapActions, kv =>
            kv.Key == "GetProductById" && kv.Value == "http://example.org/OverloadedService/GetProductById");
        Assert.Contains(binding.OperationSoapActions, kv =>
            kv.Key == "GetProductByCode" && kv.Value == "http://example.org/OverloadedService/GetProductByCode");
        Assert.Contains(binding.OperationSoapActions, kv =>
            kv.Key == "GetProductByName" && kv.Value == "http://example.org/OverloadedService/GetProductByName");
    }

    #endregion

    #region Services Tests

    [Fact]
    public void Parse_SimpleWsdl_ParsesServicesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_simpleWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Services);
        Assert.Single(result.Services);

        var service = result.Services.First();
        Assert.Equal("SimpleService", service.Name);

        Assert.NotEmpty(service.Ports);
        Assert.Single(service.Ports);

        var port = service.Ports.First();
        Assert.Equal("SimpleServicePort", port.Name);
        Assert.Equal("tns:SimpleServiceSoapBinding", port.Binding);
        Assert.Equal("http://example.org/SimpleService", port.Location);
    }

    [Fact]
    public void Parse_ComplexWsdl_ParsesServicesCorrectly()
    {
        // Arrange
        var wsdlContent = File.ReadAllText(_complexWsdlPath);

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotEmpty(result.Services);
        Assert.Single(result.Services);

        var service = result.Services.First();
        Assert.Equal("UserService", service.Name);

        Assert.NotEmpty(service.Ports);
        Assert.Single(service.Ports);

        var port = service.Ports.First();
        Assert.Equal("UserServicePort", port.Name);
        Assert.Equal("tns:UserServiceSoapBinding", port.Binding);
        Assert.Equal("http://example.org/UserService", port.Location);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Parse_EmptyWsdl_ReturnsEmptyDefinition()
    {
        // Arrange
        var wsdlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
             xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/""
             xmlns:tns=""http://example.org/EmptyService/""
             xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
             targetNamespace=""http://example.org/EmptyService/"">
</definitions>";

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://example.org/EmptyService/", result.TargetNamespace);
        Assert.NotEmpty(result.Namespaces);
        Assert.Empty(result.Types);
        Assert.Empty(result.Operations);
        Assert.Empty(result.Bindings);
        Assert.Empty(result.Services);
    }

    [Fact]
    public void Parse_MissingTargetNamespace_ReturnsEmptyTargetNamespace()
    {
        // Arrange
        var wsdlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<definitions xmlns=""http://schemas.xmlsoap.org/wsdl/""
             xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/""
             xmlns:tns=""http://example.org/EmptyService/""
             xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
</definitions>";

        // Act
        var result = _parser.Parse(wsdlContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.TargetNamespace);
    }

    #endregion
}
