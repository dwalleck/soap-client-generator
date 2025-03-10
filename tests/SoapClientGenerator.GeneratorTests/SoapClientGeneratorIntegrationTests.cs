using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Generator = SoapClientGenerator.DotnetClientGenerator;
using Xunit;

namespace SoapClientGenerator.GeneratorTests;

public class SoapClientGeneratorIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Mock<ILogger<Generator.SoapClientGenerator>> _loggerMock;
    private readonly Generator.SoapClientGeneratorOptions _defaultOptions;

    public SoapClientGeneratorIntegrationTests()
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
    public async Task GenerateClientAsync_SimpleWsdl_GeneratesCompilableCode()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("SimpleService.wsdl");
        var generator = new Generator.SoapClientGenerator(_defaultOptions, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        // Check if the project files were generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "TestNamespace.csproj"));
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "TestNamespace.sln"));

        // Verify the project can be built
        var buildResult = await BuildGeneratedProject();
        Assert.True(buildResult, "The generated project should build successfully");
    }

    [Fact]
    public async Task GenerateClientAsync_ComplexWsdl_GeneratesCompilableCode()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("ComplexService.wsdl");
        var generator = new Generator.SoapClientGenerator(_defaultOptions, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        // Check if the project files were generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "TestNamespace.csproj"));
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "TestNamespace.sln"));

        // Verify the project can be built
        var buildResult = await BuildGeneratedProject();
        Assert.True(buildResult, "The generated project should build successfully");
    }

    [Fact]
    public async Task GenerateClientAsync_OverloadedWsdl_GeneratesCompilableCode()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("OverloadedService.wsdl");
        var generator = new Generator.SoapClientGenerator(_defaultOptions, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        // Check if the project files were generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "TestNamespace.csproj"));
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "TestNamespace.sln"));

        // Verify the project can be built
        var buildResult = await BuildGeneratedProject();
        Assert.True(buildResult, "The generated project should build successfully");
    }

    [Fact]
    public async Task GenerateClientAsync_WithAllOptions_GeneratesExpectedStructure()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("ComplexService.wsdl");
        var options = new Generator.SoapClientGeneratorOptions
        {
            Namespace = "Custom.Namespace",
            ClientName = "CustomClient",
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
        // Check if the client class was generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "CustomClient.cs"));

        // Check if the data contracts directory was created
        Assert.True(TestHelpers.DirectoryContainsSubdirectory(_tempDirectory, "DataContracts"));

        // Check if the service contracts directory was created
        Assert.True(TestHelpers.DirectoryContainsSubdirectory(_tempDirectory, "ServiceContracts"));

        // Check if the project files were generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "CustomNamespace.csproj"));
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "CustomNamespace.sln"));

        // Check if the client class contains the expected content
        var clientFilePath = Path.Combine(_tempDirectory, "CustomClient.cs");
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "namespace Custom.Namespace"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public class CustomClient"));

        // Check if the data contracts directory contains the expected files
        var dataContractsDir = Path.Combine(_tempDirectory, "DataContracts");
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "UserType.cs"));
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "AddressType.cs"));
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "StatusEnum.cs"));
        Assert.True(TestHelpers.DirectoryContainsFile(dataContractsDir, "RoleEnum.cs"));

        // Verify the project can be built
        var buildResult = await BuildGeneratedProject();
        Assert.True(buildResult, "The generated project should build successfully");
    }

    [Fact]
    public async Task GenerateClientAsync_WithMinimalOptions_GeneratesExpectedStructure()
    {
        // Arrange
        var wsdlPath = TestHelpers.GetTestResourcePath("SimpleService.wsdl");
        var options = new Generator.SoapClientGeneratorOptions
        {
            Namespace = "Minimal.Namespace",
            ClientName = "MinimalClient",
            GenerateAsyncMethods = false,
            GenerateXmlComments = false,
            GenerateDataContracts = true,
            GenerateServiceContracts = true
        };
        var generator = new Generator.SoapClientGenerator(options, _loggerMock.Object);

        // Act
        await generator.GenerateClientAsync(wsdlPath, _tempDirectory);

        // Assert
        // Check if the client class was generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "MinimalClient.cs"));

        // Check if the data contracts directory was created
        Assert.True(TestHelpers.DirectoryContainsSubdirectory(_tempDirectory, "DataContracts"));

        // Check if the service contracts directory was created
        Assert.True(TestHelpers.DirectoryContainsSubdirectory(_tempDirectory, "ServiceContracts"));

        // Check if the project files were generated
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "MinimalNamespace.csproj"));
        Assert.True(TestHelpers.DirectoryContainsFile(_tempDirectory, "MinimalNamespace.sln"));

        // Check if the client class contains the expected content
        var clientFilePath = Path.Combine(_tempDirectory, "MinimalClient.cs");
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "namespace Minimal.Namespace"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public class MinimalClient"));
        Assert.False(TestHelpers.FileContainsText(clientFilePath, "/// <summary>"));
        Assert.True(TestHelpers.FileContainsText(clientFilePath, "public GetDataResponse GetData"));
        Assert.False(TestHelpers.FileContainsText(clientFilePath, "public async Task<GetDataResponse> GetDataAsync"));

        // Verify the project can be built
        var buildResult = await BuildGeneratedProject();
        Assert.True(buildResult, "The generated project should build successfully");
    }

    private async Task<bool> BuildGeneratedProject()
    {
        try
        {
            // Find the csproj file
            var projectFiles = Directory.GetFiles(_tempDirectory, "*.csproj");
            if (projectFiles.Length == 0)
            {
                return false;
            }

            var projectFile = projectFiles[0];

            // Use dotnet build to build the project
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{projectFile}\" -c Release",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
