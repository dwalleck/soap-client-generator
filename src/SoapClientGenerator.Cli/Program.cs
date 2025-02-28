using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using SoapClientGenerator.Generator;

namespace SoapClientGenerator.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<GenerateCommand>();
        
        app.Configure(config =>
        {
            config.SetApplicationName("soap-client-generator");
            config.SetApplicationVersion("1.0.0");
            
            config.AddExample(new[] { "generate", "--wsdl", "path/to/service.wsdl", "--output", "output/directory" });
            config.AddExample(new[] { "generate", "--wsdl", "path/to/service.wsdl", "--output", "output/directory", "--namespace", "MyCompany.SoapClient" });
            
            config.SetExceptionHandler((ex, _) =>
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return -1;
            });
        });
        
        return app.Run(args);
    }
}

public class GenerateCommandSettings : CommandSettings
{
    [CommandArgument(0, "[wsdl]")]
    [Description("Path to the WSDL file")]
    public string? WsdlPath { get; set; }
    
    [CommandOption("-o|--output")]
    [Description("Output directory for the generated client")]
    public string OutputDirectory { get; set; } = "output";
    
    [CommandOption("-n|--namespace")]
    [Description("Namespace for the generated client")]
    public string Namespace { get; set; } = "GeneratedSoapClient";
    
    [CommandOption("-c|--client-name")]
    [Description("Name of the generated client class")]
    public string ClientName { get; set; } = "SoapClient";
    
    [CommandOption("--async")]
    [Description("Generate async methods")]
    [DefaultValue(true)]
    public bool GenerateAsyncMethods { get; set; } = true;
    
    [CommandOption("--xml-comments")]
    [Description("Generate XML comments")]
    [DefaultValue(true)]
    public bool GenerateXmlComments { get; set; } = true;
    
    [CommandOption("--retry-count")]
    [Description("Number of retry attempts for resilience")]
    [DefaultValue(3)]
    public int RetryCount { get; set; } = 3;
    
    [CommandOption("--retry-delay")]
    [Description("Delay between retry attempts in milliseconds")]
    [DefaultValue(1000)]
    public int RetryDelay { get; set; } = 1000;
    
    [CommandOption("--exponential-backoff")]
    [Description("Use exponential backoff for retries")]
    [DefaultValue(true)]
    public bool UseExponentialBackoff { get; set; } = true;
    
    [CommandOption("--verbose")]
    [Description("Enable verbose logging")]
    public bool Verbose { get; set; }
    
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(WsdlPath))
        {
            return ValidationResult.Error("WSDL path is required");
        }
        
        if (!File.Exists(WsdlPath))
        {
            return ValidationResult.Error($"WSDL file not found: {WsdlPath}");
        }
        
        return ValidationResult.Success();
    }
}

public class GenerateCommand : AsyncCommand<GenerateCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GenerateCommandSettings settings)
    {
        // Create a logger factory
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            
            if (settings.Verbose)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(LogLevel.Information);
            }
        });
        
        var logger = loggerFactory.CreateLogger<SoapClientGenerator.Generator.SoapClientGenerator>();
        
        // Show a spinner while generating
        return await AnsiConsole.Status()
            .StartAsync("Generating SOAP client...", async ctx =>
            {
                try
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.Status("Parsing WSDL...");
                    
                    // Create generator options
                    var options = new SoapClientGeneratorOptions
                    {
                        Namespace = settings.Namespace,
                        ClientName = settings.ClientName,
                        GenerateAsyncMethods = settings.GenerateAsyncMethods,
                        GenerateXmlComments = settings.GenerateXmlComments,
                        ResilienceOptions = new ResilienceOptions
                        {
                            MaxRetryAttempts = settings.RetryCount,
                            RetryDelayMilliseconds = settings.RetryDelay,
                            UseExponentialBackoff = settings.UseExponentialBackoff
                        }
                    };
                    
                    // Create the generator
                    var generator = new SoapClientGenerator.Generator.SoapClientGenerator(options, logger);
                    
                    // Ensure output directory exists
                    Directory.CreateDirectory(settings.OutputDirectory);
                    
                    ctx.Status("Generating client code...");
                    
                    // Generate the client
                    await generator.GenerateClientAsync(settings.WsdlPath!, settings.OutputDirectory);
                    
                    // Show success message
                    AnsiConsole.MarkupLine($"[green]SOAP client generated successfully in [blue]{settings.OutputDirectory}[/][/]");
                    
                    return 0;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    
                    if (settings.Verbose)
                    {
                        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes | ExceptionFormats.ShowLinks);
                    }
                    
                    return 1;
                }
            });
    }
}
