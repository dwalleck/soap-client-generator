﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SoapClientGenerator.Generator;

class Program
{
    static async Task Main(string[] args)
    {
        // Create a logger factory
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger<SoapClientGenerator.Generator.SoapClientGenerator>();

        // Create generator options
        var options = new SoapClientGeneratorOptions
        {
            Namespace = "ACH.Client",
            ClientName = "AchClient",
            GenerateAsyncMethods = true,
            GenerateXmlComments = true,
            ResilienceOptions = new ResilienceOptions
            {
                MaxRetryAttempts = 3,
                RetryDelayMilliseconds = 1000,
                UseExponentialBackoff = true
            }
        };

        // Create the generator
        var generator = new SoapClientGenerator.Generator.SoapClientGenerator(options, logger);

        try
        {
            // Generate the client
            await generator.GenerateClientAsync("src/SoapClientGenerator.Parser/ACH.wsdl", "test-output");

            Console.WriteLine("SOAP client generated successfully in test-output");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
