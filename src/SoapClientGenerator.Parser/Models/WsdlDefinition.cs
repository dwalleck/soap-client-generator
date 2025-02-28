namespace SoapClientGenerator.Parser.Models;

/// <summary>
/// Represents a WSDL definition parsed from a WSDL document.
/// </summary>
public class WsdlDefinition
{
    /// <summary>
    /// Gets or sets the target namespace of the WSDL.
    /// </summary>
    public required string TargetNamespace { get; set; }

    /// <summary>
    /// Gets or sets the namespaces defined in the WSDL.
    /// </summary>
    public IDictionary<string, string> Namespaces { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the collection of types defined in the WSDL.
    /// </summary>
    public ICollection<WsdlType> Types { get; set; } = new List<WsdlType>();

    /// <summary>
    /// Gets or sets the collection of operations defined in the WSDL.
    /// </summary>
    public ICollection<WsdlOperation> Operations { get; set; } = new List<WsdlOperation>();

    /// <summary>
    /// Gets or sets the collection of bindings defined in the WSDL.
    /// </summary>
    public ICollection<WsdlBinding> Bindings { get; set; } = new List<WsdlBinding>();

    /// <summary>
    /// Gets or sets the collection of services defined in the WSDL.
    /// </summary>
    public ICollection<WsdlService> Services { get; set; } = new List<WsdlService>();
}

/// <summary>
/// Represents a binding in a WSDL document, which defines the message format and protocol details for operations.
/// </summary>
public class WsdlBinding
{
    /// <summary>
    /// Gets or sets the name of the binding.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the port type that this binding is associated with.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the transport protocol used by this binding.
    /// </summary>
    public required string Transport { get; set; }

    /// <summary>
    /// Gets or sets a dictionary mapping operation names to their SOAP actions.
    /// </summary>
    public IDictionary<string, string> OperationSoapActions { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a service in a WSDL document, which is a collection of related ports.
/// </summary>
public class WsdlService
{
    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the collection of ports defined in this service.
    /// </summary>
    public ICollection<WsdlPort> Ports { get; set; } = new List<WsdlPort>();
}

/// <summary>
/// Represents a port in a WSDL document, which defines a single endpoint by specifying a network address for a binding.
/// </summary>
public class WsdlPort
{
    /// <summary>
    /// Gets or sets the name of the port.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the binding that this port is associated with.
    /// </summary>
    public required string Binding { get; set; }

    /// <summary>
    /// Gets or sets the network address where the service can be accessed.
    /// </summary>
    public required string Location { get; set; }
}
