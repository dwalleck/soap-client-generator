using Xunit;
using SoapClientGenerator.Parser.Models;
using System.IO;

namespace SoapClientGenerator.Parser.Tests;

public class WsdlParserTests
{
    private readonly WsdlParser _parser;
    private readonly string _wsdlContent;

    public WsdlParserTests()
    {
        _parser = new WsdlParser();
        _wsdlContent = File.ReadAllText("ACH.wsdl");
    }

    [Fact]
    public void Parse_ShouldParseTargetNamespace()
    {
        var wsdl = _parser.Parse(_wsdlContent);
        Assert.Equal("http://www.swbc.com/", wsdl.TargetNamespace);
    }

    [Fact]
    public void Parse_ShouldParseNamespaces()
    {
        var wsdl = _parser.Parse(_wsdlContent);
        Assert.Contains(wsdl.Namespaces, ns => ns.Key == "s" && ns.Value == "http://www.w3.org/2001/XMLSchema");
        Assert.Contains(wsdl.Namespaces, ns => ns.Key == "soap" && ns.Value == "http://schemas.xmlsoap.org/wsdl/soap/");
    }

    [Fact]
    public void Parse_ShouldParseComplexTypes()
    {
        var wsdl = _parser.Parse(_wsdlContent);
        var achTransResponse = wsdl.Types.FirstOrDefault(t => t.Name == "ACHTransResponse");

        Assert.NotNull(achTransResponse);
        Assert.Equal(WsdlTypeKind.Complex, achTransResponse.Kind);
        Assert.Equal("http://www.swbc.com/", achTransResponse.Namespace);

        var properties = achTransResponse.Properties.ToList();
        Assert.Equal(3, properties.Count);
        Assert.Contains(properties, p => p.Name == "ResponseCode" && !p.IsRequired);
        Assert.Contains(properties, p => p.Name == "ResponseMessage" && !p.IsRequired);
        Assert.Contains(properties, p => p.Name == "ResponseStringRaw" && !p.IsRequired);
    }

    [Fact]
    public void Parse_ShouldParseEnumTypes()
    {
        var wsdl = _parser.Parse(_wsdlContent);
        var paymentSource = wsdl.Types.FirstOrDefault(t => t.Name == "PaymentSource");

        Assert.NotNull(paymentSource);
        Assert.Equal(WsdlTypeKind.Enum, paymentSource.Kind);
        Assert.True(paymentSource.IsEnum);

        var enumValues = paymentSource.EnumValues.ToList();
        Assert.Contains("OnlineAkcelerant", enumValues);
        Assert.Contains("OnlineECM", enumValues);
        Assert.Contains("OnlineWeblet", enumValues);
    }

    [Fact]
    public void Parse_ShouldParseOperations()
    {
        var wsdl = _parser.Parse(_wsdlContent);

        // Test basic operation
        var basicOperation = wsdl.Operations.FirstOrDefault(o => o.Name == "PostSinglePayment");
        Assert.NotNull(basicOperation);
        Assert.Equal("http://www.swbc.com/PostSinglePayment", basicOperation.SoapAction);
        Assert.Equal("Post a Single ACH transaction. Returns ACHTransResponse Class", basicOperation.Documentation);

        // Test operation with payment source
        var paymentSourceOperation = wsdl.Operations.FirstOrDefault(o => o.Name == "PostSinglePaymentWithPaymentSource");
        Assert.NotNull(paymentSourceOperation);
        Assert.Equal("http://www.swbc.com/PostSinglePaymentWithPaymentSource", paymentSourceOperation.SoapAction);

        // Test operation with apply to
        var applyToOperation = wsdl.Operations.FirstOrDefault(o => o.Name == "PostSinglePaymentWithApplyTo");
        Assert.NotNull(applyToOperation);
        Assert.Equal("http://www.swbc.com/PostSinglePaymentWithApplyTo", applyToOperation.SoapAction);
    }

    [Fact]
    public void Parse_ShouldDetectAuthHeaders()
    {
        var wsdl = _parser.Parse(_wsdlContent);

        // Verify that the SWBCAuthHeader type exists
        var authHeaderType = wsdl.Types.FirstOrDefault(t => t.Name == "SWBCAuthHeader");
        Assert.NotNull(authHeaderType);
        Assert.Equal(WsdlTypeKind.Complex, authHeaderType.Kind);

        // Verify the auth header properties
        var properties = authHeaderType.Properties.ToList();
        Assert.Equal(2, properties.Count);
        Assert.Contains(properties, p => p.Name == "Username" && !p.IsRequired);
        Assert.Contains(properties, p => p.Name == "Password" && !p.IsRequired);
    }

    [Fact]
    public void Parse_ShouldAssociateAuthHeadersWithOperations()
    {
        var wsdl = _parser.Parse(_wsdlContent);

        // Check that operations have auth headers
        var operations = wsdl.Operations.ToList();

        // All operations except validation operations should require auth
        var operationsRequiringAuth = operations.Where(o =>
            !o.Name.StartsWith("ValidateABA") &&
            !o.Name.StartsWith("GetABA") &&
            !o.Name.StartsWith("GetStatus") &&
            !o.Name.StartsWith("TestF5")).ToList();

        foreach (var operation in operationsRequiringAuth)
        {
            Assert.NotNull(operation.AuthHeader);
            // The auth header name includes the operation name as a prefix
            Assert.Contains("SWBCAuthHeader", operation.AuthHeader.Name);
            Assert.Equal("SWBCAuthHeader", operation.AuthHeader.TypeName);
        }

        // Validation operations should not require auth
        var validateOperation = operations.FirstOrDefault(o => o.Name == "ValidateABANumber");
        Assert.NotNull(validateOperation);
        Assert.Null(validateOperation.AuthHeader);

        var getAbaOperation = operations.FirstOrDefault(o => o.Name == "GetABACompletionList");
        Assert.NotNull(getAbaOperation);
        Assert.Null(getAbaOperation.AuthHeader);
    }

    [Fact]
    public void Parse_ShouldParseBindings()
    {
        var wsdl = _parser.Parse(_wsdlContent);
        var binding = wsdl.Bindings.FirstOrDefault(b => b.Name == "ACHTransactionSoap");

        Assert.NotNull(binding);
        Assert.Equal("http://schemas.xmlsoap.org/soap/http", binding.Transport);

        // Verify all operation variants are present with correct SOAP actions
        var soapActions = binding.OperationSoapActions;
        Assert.Contains(soapActions, action => action.Value == "http://www.swbc.com/PostSinglePayment");
        Assert.Contains(soapActions, action => action.Value == "http://www.swbc.com/PostSinglePaymentWithPaymentSource");
        Assert.Contains(soapActions, action => action.Value == "http://www.swbc.com/PostSinglePaymentWithApplyTo");
    }

    [Fact]
    public void Parse_ShouldParseServices()
    {
        var wsdl = _parser.Parse(_wsdlContent);
        var service = wsdl.Services.FirstOrDefault(s => s.Name == "ACHTransaction");

        Assert.NotNull(service);
        var port = service.Ports.FirstOrDefault(p => p.Name == "ACHTransactionSoap");
        Assert.NotNull(port);
        Assert.Equal("http://localhost:2785/ach.asmx", port.Location);
    }
}
