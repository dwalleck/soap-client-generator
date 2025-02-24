namespace SoapClientGenerator.Parser.Models;

public class WsdlDefinition
{
    public required string TargetNamespace { get; set; }
    public IDictionary<string, string> Namespaces { get; set; } = new Dictionary<string, string>();
    public ICollection<WsdlType> Types { get; set; } = new List<WsdlType>();
    public ICollection<WsdlOperation> Operations { get; set; } = new List<WsdlOperation>();
    public ICollection<WsdlBinding> Bindings { get; set; } = new List<WsdlBinding>();
    public ICollection<WsdlService> Services { get; set; } = new List<WsdlService>();
}

public class WsdlBinding
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Transport { get; set; }
    public IDictionary<string, string> OperationSoapActions { get; set; } = new Dictionary<string, string>();
}

public class WsdlService
{
    public required string Name { get; set; }
    public ICollection<WsdlPort> Ports { get; set; } = new List<WsdlPort>();
}

public class WsdlPort
{
    public required string Name { get; set; }
    public required string Binding { get; set; }
    public required string Location { get; set; }
}
