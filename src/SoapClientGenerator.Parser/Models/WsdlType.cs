namespace SoapClientGenerator.Parser.Models;

public class WsdlType
{
    public required string Name { get; set; }
    public required string Namespace { get; set; }
    public required WsdlTypeKind Kind { get; set; }
    public ICollection<WsdlTypeProperty> Properties { get; set; } = new List<WsdlTypeProperty>();
    public string? BaseType { get; set; }
    public bool IsEnum { get; set; }
    public ICollection<string> EnumValues { get; set; } = new List<string>();
}

public class WsdlTypeProperty
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public bool IsRequired { get; set; }
    public bool IsCollection { get; set; }
    public string? Documentation { get; set; }
}

public enum WsdlTypeKind
{
    Simple,
    Complex,
    Enum
}
