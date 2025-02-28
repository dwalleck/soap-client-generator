using System;

namespace SoapClientGenerator.Generator;

/// <summary>
/// Configuration options for the SOAP client generator
/// </summary>
public class SoapClientGeneratorOptions
{
    /// <summary>
    /// Gets or sets the namespace for the generated client
    /// </summary>
    public string Namespace { get; set; } = "GeneratedSoapClient";

    /// <summary>
    /// Gets or sets the name of the generated client class
    /// </summary>
    public string ClientName { get; set; } = "SoapClient";

    /// <summary>
    /// Gets or sets whether to generate async methods
    /// </summary>
    public bool GenerateAsyncMethods { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate XML comments
    /// </summary>
    public bool GenerateXmlComments { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate data contracts
    /// </summary>
    public bool GenerateDataContracts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate service contracts
    /// </summary>
    public bool GenerateServiceContracts { get; set; } = true;

    /// <summary>
    /// Gets or sets the resilience options
    /// </summary>
    public ResilienceOptions? ResilienceOptions { get; set; } = new ResilienceOptions();

    /// <summary>
    /// Gets or sets the telemetry options
    /// </summary>
    public TelemetryOptions? TelemetryOptions { get; set; } = new TelemetryOptions();
}

/// <summary>
/// Configuration options for resilience
/// </summary>
public class ResilienceOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for operations in milliseconds
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 30000;

    /// <summary>
    /// Gets or sets whether to use circuit breaker
    /// </summary>
    public bool UseCircuitBreaker { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of exceptions before opening the circuit
    /// </summary>
    public int CircuitBreakerExceptionsAllowedBeforeBreaking { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration the circuit will stay open before resetting in milliseconds
    /// </summary>
    public int CircuitBreakerDurationOfBreakMilliseconds { get; set; } = 30000;
}

/// <summary>
/// Configuration options for telemetry
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// Gets or sets whether to enable telemetry
    /// </summary>
    public bool EnableTelemetry { get; set; } = false;

    /// <summary>
    /// Gets or sets the service name for telemetry
    /// </summary>
    public string ServiceName { get; set; } = "SoapClientGenerator";

    /// <summary>
    /// Gets or sets the service version for telemetry
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets whether to track dependencies
    /// </summary>
    public bool TrackDependencies { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track exceptions
    /// </summary>
    public bool TrackExceptions { get; set; } = true;
}
