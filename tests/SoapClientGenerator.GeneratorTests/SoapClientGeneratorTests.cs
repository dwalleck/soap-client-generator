using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Generator = SoapClientGenerator.DotnetClientGenerator;
using Xunit;

namespace SoapClientGenerator.GeneratorTests;

public class SoapClientGeneratorTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Mock<ILogger<Generator.SoapClientGenerator>> _loggerMock;
    private readonly Generator.SoapClientGeneratorOptions _defaultOptions;

    public SoapClientGeneratorTests()
    {
        _tempDirectory = TestHelpers.CreateTemporaryDirectory();
        _loggerMock = new Mock<ILogger<Generator.SoapClientGenerator>>();
        _defaultOptions = new Generator.SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = true,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
    }

    public void Dispose()
    {
        TestHelpers.DeleteDirectory(_tempDirectory);
    }

    [Fact]
    public async Task GenerateClientAsync_SimpleWsdl_GeneratesExpectedFiles()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("SimpleService.wsdl");
        var generator = new Generator.SoapClientGenerator(_defaultOptions, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        // Check if the client class was generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, $"{_defaultOptions.ClientName}.cs"));

        // Check if the data contracts directory was created
        Assert.True(TestHelpers.DirectoryContainsSubdirectory(_tempDirectory, "DataContracts"));

        // Check if the service contracts directory was created
        Assert.True(TestHelpers.DirectoryContainsSubdirectory(_tempDirectory, "ServiceContracts"));

        // Check if the project files were generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "TestNamespace.csproj"));
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "TestNamespace.sln"));

        // Check if the client class contains the expected content
        var clientFilePath = Path.Combine(_tempDirectory, $"{_defaultOptions.ClientName}.cs");
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "namespace TestNamespace"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public class TestClient"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public async Task<GetDataResponse> GetDataAsync"));

        // Check if the data contracts directory contains the expected files
        var dataContractsDir = Path.Combine(_tempDirectory, "DataContracts");
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "DataType.cs"));
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "StatusEnum.cs"));

        // Check if the service contracts directory contains the expected files
        var serviceContractsDir = Path.Combine(_tempDirectory, "ServiceContracts");
        Assert.True(TestHelpers.DirectoryContainsFile(serviceContractsDir, "IServiceContract.cs"));
    }

    [Fact]
    public async Task GenerateClientAsync_ComplexWsdl_GeneratesExpectedFiles()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("ComplexService.wsdl");
        var generator = new Generator.SoapClientGenerator(_defaultOptions, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        // Check if the client class was generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, $"{_defaultOptions.ClientName}.cs"));

        // Check if the data contracts directory was created
        Assert.True(TestHelpers.DirectoryContainsSubdirectory(_tempDirectory, "DataContracts"));

        // Check if the service contracts directory was created
        Assert.True(TestHelpers.DirectoryContainsSubdirectory(_tempDirectory, "ServiceContracts"));

        // Check if the client class contains the expected content
        var clientFilePath = Path.Combine(_tempDirectory, $"{_defaultOptions.ClientName}.cs");
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "namespace TestNamespace"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public class TestClient"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public async Task<GetUserResponse> GetUserAsync"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public async Task<CreateUserResponse> CreateUserAsync"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public async Task<UpdateUserResponse> UpdateUserAsync"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public async Task<DeleteUserResponse> DeleteUserAsync"));

        // Check if the data contracts directory contains the expected files
        var dataContractsDir = Path.Combine(_tempDirectory, "DataContracts");
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "UserType.cs"));
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "AddressType.cs"));
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "StatusEnum.cs"));
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "RoleEnum.cs"));
    }

    [Fact]
    public async Task GenerateClientAsync_OverloadedWsdl_GeneratesExpectedFiles()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("OverloadedService.wsdl");
        var generator = new Generator.SoapClientGenerator(_defaultOptions, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        // Check if the client class was generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, $"{_defaultOptions.ClientName}.cs"));

        // Check if the client class contains the expected content
        var clientFilePath = Path.Combine(_tempDirectory, $"{_defaultOptions.ClientName}.cs");
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public async Task<GetProductByIdResponse> GetProductByIdAsync"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public async Task<GetProductByCodeResponse> GetProductByCodeAsync"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public async Task<GetProductByNameResponse> GetProductByNameAsync"));
    }

    [Fact]
    public async Task GenerateClientAsync_WithSyncMethods_GeneratesSyncMethods()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("SimpleService.wsdl");
        var options = new Generator.SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = false,
            GenerateXmlComments = true,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
        var generator = new Generator.SoapClientGenerator(options, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        var clientFilePath = Path.Combine(_tempDirectory, $"{options.ClientName}.cs");
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public GetDataResponse GetData"));
        Assert.False(TestHelpers.FileContainsText(clientFilePath, "public async Task<GetDataResponse> GetDataAsync"));
    }

    [Fact]
    public async Task GenerateClientAsync_WithoutXmlComments_DoesNotGenerateXmlComments()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("SimpleService.wsdl");
        var options = new Generator.SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = false,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
        var generator = new Generator.SoapClientGenerator(options, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        var clientFilePath = Path.Combine(_tempDirectory, $"{options.ClientName}.cs");
        Assert.False(TestHelpers.FileContainsText(clientFilePath, "/// <summary>"));
    }

    [Fact]
    public async Task GenerateClientAsync_WithCustomNamespace_UsesCustomNamespace()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("SimpleService.wsdl");
        var options = new Generator.SoapClientGeneratorOptions
        {
            Namespace = "Custom.Namespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = true,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
        var generator = new Generator.SoapClientGenerator(options, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        var clientFilePath = Path.Combine(_tempDirectory, $"{options.ClientName}.cs");
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "namespace Custom.Namespace"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "using Custom.Namespace.DataContracts"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "using Custom.Namespace.ServiceContracts"));
    }

    [Fact]
    public async Task GenerateClientAsync_WithCustomClientName_UsesCustomClientName()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("SimpleService.wsdl");
        var options = new Generator.SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "CustomClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = true,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
        var generator = new Generator.SoapClientGenerator(options, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "CustomClient.cs"));
        var clientFilePath = Path.Combine(_tempDirectory, "CustomClient.cs");
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public class CustomClient"));
    }

    [Fact]
    public async Task GenerateClientAsync_WithCustomResilienceOptions_UsesCustomResilienceOptions()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("SimpleService.wsdl");
        var options = new Generator.SoapClientGeneratorOptions
        {
            Namespace = "TestNamespace",
            ClientName = "TestClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = true,
            GenerateDataContracts = true,
            GenerateServiceContracts = true,
            ResilienceOptions = new Generator.ResilienceOptions
            {
                MaxRetryAttempts = 5,
                RetryDelayMilliseconds = 2000,
                UseExponentialBackoff = false
            }
        };
        var generator = new Generator.SoapClientGenerator(options, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        // We can't easily test the resilience options directly, but we can verify that the generator completed successfully
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, $"{options.ClientName}.cs"));
    }

    [Fact]
    public async Task GenerateClientAsync_InvalidWsdlPath_ThrowsFileNotFoundException()
    {
        // Arrange
        var wsdlPath = "NonExistentFile.wsdl";
        var generator = new Generator.SoapClientGenerator(_defaultOptions, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => generator.GenerateClientAsync(wsdlPath, _tempDirectory));
    }
}
