using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SoapClientGenerator.Generator.Templates;
using SoapClientGenerator.Parser;
using SoapClientGenerator.Parser.Models;

namespace SoapClientGenerator.Generator;

/// <summary>
/// Main class for generating SOAP clients from WSDL files
/// </summary>
public class SoapClientGenerator
{
    private readonly WsdlParser _parser;
    private readonly ILogger<SoapClientGenerator>? _logger;
    private readonly SoapClientGeneratorOptions _options;
    private readonly ResilienceOptions _resilienceOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoapClientGenerator"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the generator</param>
    /// <param name="logger">Optional logger for logging</param>
    public SoapClientGenerator(SoapClientGeneratorOptions options, ILogger<SoapClientGenerator>? logger = null)
    {
        _parser = new WsdlParser();
        _logger = logger;
        _options = options ?? new SoapClientGeneratorOptions();
        _resilienceOptions = _options.ResilienceOptions ?? new ResilienceOptions();
    }

    /// <summary>
    /// Generates a SOAP client from a WSDL file
    /// </summary>
    /// <param name="wsdlPath">Path to the WSDL file</param>
    /// <param name="outputDirectory">Directory where the generated client will be saved</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task GenerateClientAsync(string wsdlPath, string outputDirectory)
    {
        _logger?.LogInformation("Starting SOAP client generation from WSDL: {WsdlPath}", wsdlPath);

        try
        {
            // Create a resilience pipeline for file operations
            var fileOperationsPolicy = CreateFileOperationsPolicy();

            // Read the WSDL file with resilience
            string wsdlContent = await fileOperationsPolicy.ExecuteAsync(
                async (context, cancellationToken) => await File.ReadAllTextAsync(wsdlPath, cancellationToken),
                new Polly.Context());

            // Parse the WSDL
            WsdlDefinition wsdl = _parser.Parse(wsdlContent);
            _logger?.LogInformation("Successfully parsed WSDL with {OperationCount} operations", wsdl.Operations.Count);

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Generate client code
            await GenerateClientCodeAsync(wsdl, outputDirectory);

            // Generate project files
            await GenerateProjectFilesAsync(outputDirectory);

            _logger?.LogInformation("SOAP client generation completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating SOAP client: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    private async Task GenerateProjectFilesAsync(string outputDirectory)
    {
        // Generate csproj file
        await GenerateCsprojFileAsync(outputDirectory);

        // Generate solution file
        await GenerateSolutionFileAsync(outputDirectory);
    }

    private async Task GenerateCsprojFileAsync(string outputDirectory)
    {
        string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{_options.Namespace}</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""System.ServiceModel.Http"" Version=""6.2.0"" />
    <PackageReference Include=""System.ServiceModel.Primitives"" Version=""6.2.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Abstractions"" Version=""8.0.0"" />
    <PackageReference Include=""Polly"" Version=""8.2.0"" />
  </ItemGroup>

</Project>
";

        string projectName = _options.Namespace.Replace(".", string.Empty);
        string csprojPath = Path.Combine(outputDirectory, $"{projectName}.csproj");
        await File.WriteAllTextAsync(csprojPath, csprojContent);
    }

    private async Task GenerateSolutionFileAsync(string outputDirectory)
    {
        string projectName = _options.Namespace.Replace(".", string.Empty);
        // Generate a GUID for the project
        string projectGuid = Guid.NewGuid().ToString("B").ToUpper();

        string slnContent = $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}"", ""{projectName}.csproj"", ""{projectGuid}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
";

        string slnPath = Path.Combine(outputDirectory, $"{projectName}.sln");
        await File.WriteAllTextAsync(slnPath, slnContent);
    }

    private async Task GenerateClientCodeAsync(WsdlDefinition wsdl, string outputDirectory)
    {
        // Generate client class
        await GenerateClientClassAsync(wsdl, outputDirectory);

        // Generate data contracts (types)
        await GenerateDataContractsAsync(wsdl, outputDirectory);

        // Generate service contracts (operations)
        await GenerateServiceContractsAsync(wsdl, outputDirectory);
    }

    private async Task GenerateClientClassAsync(WsdlDefinition wsdl, string outputDirectory)
    {
        _logger?.LogInformation("Generating client class");

        // Get the service name from the WSDL
        string serviceName = wsdl.Services.FirstOrDefault()?.Name ?? "SoapService";

        // Generate the client class code using the template
        string clientCode = ClientTemplate.GenerateClientClass(_options, serviceName, wsdl.Operations);

        // Write the client class to a file
        string clientFilePath = Path.Combine(outputDirectory, $"{_options.ClientName}.cs");
        await File.WriteAllTextAsync(clientFilePath, clientCode);
    }

    private async Task GenerateDataContractsAsync(WsdlDefinition wsdl, string outputDirectory)
    {
        _logger?.LogInformation("Generating data contracts for {TypeCount} types", wsdl.Types.Count);

        // Create a directory for data contracts
        string dataContractsDir = Path.Combine(outputDirectory, "DataContracts");
        Directory.CreateDirectory(dataContractsDir);

        foreach (var type in wsdl.Types)
        {
            if (type.Kind == WsdlTypeKind.Complex)
            {
                // Generate a class for each complex type
                var properties = type.Properties.ToDictionary(
                    p => p.Name,
                    p => (Type: MapWsdlTypeToClrType(p.Type, p.IsCollection), IsRequired: p.IsRequired)
                );

                string classCode = ClientTemplate.GenerateDataContract(_options, type.Name, properties);
                string filePath = Path.Combine(dataContractsDir, $"{type.Name}.cs");
                await File.WriteAllTextAsync(filePath, classCode);
            }
            else if (type.Kind == WsdlTypeKind.Enum)
            {
                // Generate an enum for each enum type
                string enumCode = ClientTemplate.GenerateEnum(_options, type.Name, type.EnumValues);
                string filePath = Path.Combine(dataContractsDir, $"{type.Name}.cs");
                await File.WriteAllTextAsync(filePath, enumCode);
            }
        }
    }

    private string MapWsdlTypeToClrType(string wsdlType, bool isCollection)
    {
        // Extract the local name from the qualified name
        string localName = wsdlType;
        if (wsdlType.Contains(":"))
        {
            localName = wsdlType.Split(':')[1];
        }

        // Map XML Schema types to CLR types
        string clrType = localName switch
        {
            "string" => "string",
            "int" => "int",
            "integer" => "int",
            "long" => "long",
            "short" => "short",
            "decimal" => "decimal",
            "float" => "float",
            "double" => "double",
            "boolean" => "bool",
            "dateTime" => "DateTime",
            "date" => "DateTime",
            "time" => "TimeSpan",
            "base64Binary" => "byte[]",
            "anyURI" => "Uri",
            "QName" => "string",
            "NOTATION" => "string",
            "normalizedString" => "string",
            "token" => "string",
            "language" => "string",
            "NMTOKEN" => "string",
            "NMTOKENS" => "string[]",
            "Name" => "string",
            "NCName" => "string",
            "ID" => "string",
            "IDREF" => "string",
            "IDREFS" => "string[]",
            "ENTITY" => "string",
            "ENTITIES" => "string[]",
            "duration" => "TimeSpan",
            "gYearMonth" => "string",
            "gYear" => "string",
            "gMonthDay" => "string",
            "gDay" => "string",
            "gMonth" => "string",
            "hexBinary" => "byte[]",
            "anyType" => "object",
            _ => localName // For custom types, use the local name
        };

        // If it's a collection, wrap it in a List<>
        return isCollection ? $"List<{clrType}>" : clrType;
    }

    private async Task GenerateServiceContractsAsync(WsdlDefinition wsdl, string outputDirectory)
    {
        _logger?.LogInformation("Generating service contracts for {OperationCount} operations", wsdl.Operations.Count);

        // Create a directory for service contracts
        string serviceContractsDir = Path.Combine(outputDirectory, "ServiceContracts");
        Directory.CreateDirectory(serviceContractsDir);

        // Generate a service contract interface
        var sb = new System.Text.StringBuilder();

        // Add using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine($"using {_options.Namespace}.DataContracts;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {_options.Namespace}.ServiceContracts");
        sb.AppendLine("{");

        // Add interface
        if (_options.GenerateXmlComments)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Service contract for the SOAP service");
            sb.AppendLine("    /// </summary>");
        }
        sb.AppendLine("    public interface IServiceContract");
        sb.AppendLine("    {");

        // Add methods for each operation
        foreach (var operation in wsdl.Operations)
        {
            if (_options.GenerateXmlComments)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {operation.Documentation}");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        /// <param name=\"cancellationToken\">A cancellation token that can be used to cancel the operation</param>");
                sb.AppendLine($"        /// <returns>The response from the {operation.Name} operation</returns>");
            }

            // For now, we'll just generate placeholder methods
            // In a real implementation, we would parse the input and output messages
            if (_options.GenerateAsyncMethods)
            {
                sb.AppendLine($"        Task<object> {operation.Name}Async(CancellationToken cancellationToken = default);");
            }
            else
            {
                sb.AppendLine($"        object {operation.Name}();");
            }
            sb.AppendLine();
        }

        // Close interface and namespace
        sb.AppendLine("    }");
        sb.AppendLine("}");

        // Write the service contract to a file
        string filePath = Path.Combine(serviceContractsDir, "IServiceContract.cs");
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    private ResiliencePipeline CreateFileOperationsPolicy()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _resilienceOptions.MaxRetryAttempts,
                BackoffType = _resilienceOptions.UseExponentialBackoff ? DelayBackoffType.Exponential : DelayBackoffType.Constant,
                Delay = TimeSpan.FromMilliseconds(_resilienceOptions.RetryDelayMilliseconds),
                OnRetry = args =>
                {
                    _logger?.LogWarning("Retry attempt {RetryAttempt} after error: {ErrorMessage}",
                        args.AttemptNumber, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
