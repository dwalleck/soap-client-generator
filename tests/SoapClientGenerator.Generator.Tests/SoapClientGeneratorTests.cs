using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace SoapClientGenerator.Generator.Tests;

public class SoapClientGeneratorTests
{
    private readonly Mock<ILogger<SoapClientGenerator>> _loggerMock;
    private readonly SoapClientGeneratorOptions _options;
    private readonly string _testOutputDir;

    public SoapClientGeneratorTests()
    {
        _loggerMock = new Mock<ILogger<SoapClientGenerator>>();
        _options = new SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = true,
            ResilienceOptions = new ResilienceOptions
            {
                MaxRetryAttempts = 2,
                RetryDelayMilliseconds = 100,
                UseExponentialBackoff = false
            }
        };

        // Create a temporary directory for test output
        _testOutputDir = Path.Combine(Path.GetTempPath(), "SoapClientGeneratorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);
    }

    [Fact]
    public async Task GenerateClientAsync_WithValidWsdl_ShouldGenerateClient()
    {
        // Arrange
        var wsdlPath = Path.Combine("TestData", "ACH.wsdl");
        var generator = new SoapClientGenerator(_options, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _testOutputDir);

        // Assert
        Assert.True(File.Exists(Path.Combine(_testOutputDir, $"{_options.ClientName}.cs")));
        Assert.True(Directory.Exists(Path.Combine(_testOutputDir, "DataContracts")));
        Assert.True(Directory.Exists(Path.Combine(_testOutputDir, "ServiceContracts")));
    }

    [Fact]
    public async Task GenerateClientAsync_ShouldGenerateAuthHeaderSupport()
    {
        // Arrange
        var wsdlPath = Path.Combine("TestData", "ACH.wsdl");
        var generator = new SoapClientGenerator(_options, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _testOutputDir);

        // Assert
        var clientFilePath = Path.Combine(_testOutputDir, $"{_options.ClientName}.cs");
        var clientCode = await File.ReadAllTextAsync(clientFilePath);

        // Verify auth header properties and methods
        Assert.Contains("private string? _username;", clientCode);
        Assert.Contains("private string? _password;", clientCode);
        Assert.Contains("private bool _useAuthHeader;", clientCode);
        Assert.Contains("public void SetAuthCredentials(string username, string password)", clientCode);

        // Verify CreateSoapEnvelope method with auth header support
        Assert.Contains("private XDocument CreateSoapEnvelope(string action, XElement content, bool requiresAuth = false)", clientCode);
        Assert.Contains("if (requiresAuth && _useAuthHeader)", clientCode);
        Assert.Contains("var authHeader = new XElement", clientCode);

        // Verify that operations have auth header support
        Assert.Contains("var soapEnvelope = CreateSoapEnvelope(soapAction, requestElement, true);", clientCode);
        Assert.Contains("This operation requires authentication", clientCode);

        // Verify that the SWBCAuthHeader data contract was generated
        var authHeaderFilePath = Path.Combine(_testOutputDir, "DataContracts", "SWBCAuthHeader.cs");
        Assert.True(File.Exists(authHeaderFilePath));

        var authHeaderCode = await File.ReadAllTextAsync(authHeaderFilePath);
        Assert.Contains("public class SWBCAuthHeader", authHeaderCode);
        Assert.Contains("public string Username { get; set; }", authHeaderCode);
        Assert.Contains("public string Password { get; set; }", authHeaderCode);
    }

    [Fact]
    public async Task GenerateClientAsync_WithInvalidWsdlPath_ShouldThrowException()
    {
        // Arrange
        var wsdlPath = "NonExistentFile.wsdl";
        var generator = new SoapClientGenerator(_options, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            generator.GenerateClientAsync(wsdlPath, _testOutputDir));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaultOptions()
    {
        // Arrange & Act
        var generator = new SoapClientGenerator(null!, _loggerMock.Object);

        // Assert - if no exception is thrown, the test passes
        Assert.NotNull(generator);
    }
}
