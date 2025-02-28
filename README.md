# SOAP Client Generator

A .NET tool for automatically generating strongly-typed C# clients for SOAP web services from WSDL files.

## Features

- **WSDL Parsing**: Parses WSDL files to extract service definitions, operations, and data types
- **Code Generation**: Generates strongly-typed C# client code with async support
- **Resilience Patterns**: Built-in support for retry policies, circuit breakers, and timeouts
- **Telemetry**: Optional telemetry integration for monitoring service calls
- **XML Documentation**: Generates XML comments for all generated code

## Installation

### As a .NET Tool

```bash
dotnet tool install --global SoapClientGenerator.Cli
```

### From Source

```bash
git clone https://github.com/yourusername/soap-client-generator.git
cd soap-client-generator
dotnet build
```

## Usage

### Command Line Interface

```bash
soap-client-generator generate --wsdl path/to/service.wsdl --output output/directory
```

#### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--wsdl` | Path to the WSDL file | (Required) |
| `-o, --output` | Output directory for the generated client | `output` |
| `-n, --namespace` | Namespace for the generated client | `GeneratedSoapClient` |
| `-c, --client-name` | Name of the generated client class | `SoapClient` |
| `--async` | Generate async methods | `true` |
| `--xml-comments` | Generate XML comments | `true` |
| `--retry-count` | Number of retry attempts for resilience | `3` |
| `--retry-delay` | Delay between retry attempts in milliseconds | `1000` |
| `--exponential-backoff` | Use exponential backoff for retries | `true` |
| `--verbose` | Enable verbose logging | `false` |

### Examples

Generate a client with default settings:
```bash
soap-client-generator generate --wsdl path/to/service.wsdl --output output/directory
```

Generate a client with a custom namespace:
```bash
soap-client-generator generate --wsdl path/to/service.wsdl --output output/directory --namespace MyCompany.SoapClient
```

## Project Structure

- **SoapClientGenerator.Parser**: Parses WSDL files into object models
- **SoapClientGenerator.Generator**: Generates C# code from the parsed WSDL models
- **SoapClientGenerator.Cli**: Command-line interface for the generator

## Generated Code Structure

The generator creates the following structure in the output directory:

```
OutputDirectory/
├── {Namespace}.csproj       # Project file with required dependencies
├── {Namespace}.sln          # Solution file
├── {ClientName}.cs          # Main client class
├── DataContracts/           # Data contract classes
│   ├── Type1.cs
│   ├── Type2.cs
│   └── ...
└── ServiceContracts/        # Service contract interfaces
    └── IServiceContract.cs
```

## Using the Generated Client

```csharp
// Create an HttpClient (consider using IHttpClientFactory in production)
var httpClient = new HttpClient();

// Create the SOAP client
var client = new SoapClient(httpClient, "https://example.com/soap/service");

// Create a request
var request = new GetDataRequest
{
    Id = 123
};

// Call the service
var response = await client.GetDataAsync(request);

// Use the response
Console.WriteLine($"Result: {response.Result}");
```

## Dependencies

- .NET 8.0+
- System.ServiceModel.Http
- System.ServiceModel.Primitives
- Microsoft.Extensions.Logging.Abstractions
- Polly

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
