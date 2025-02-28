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
