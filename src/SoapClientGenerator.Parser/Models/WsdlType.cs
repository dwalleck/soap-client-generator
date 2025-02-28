namespace SoapClientGenerator.Parser.Models;

/// <summary>
/// Represents a type defined in a WSDL document.
/// </summary>
public class WsdlType
{
    /// <summary>
    /// Gets or sets the name of the type.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the type.
    /// </summary>
    public required string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the kind of the type (Simple, Complex, or Enum).
    /// </summary>
    public required WsdlTypeKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the collection of properties for this type.
    /// </summary>
    public ICollection<WsdlTypeProperty> Properties { get; set; } = new List<WsdlTypeProperty>();

    /// <summary>
    /// Gets or sets the base type for this type, if any.
    /// </summary>
    public string? BaseType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this type is an enumeration.
    /// </summary>
    public bool IsEnum { get; set; }

    /// <summary>
    /// Gets or sets the collection of enumeration values for this type, if it is an enumeration.
    /// </summary>
    public ICollection<string> EnumValues { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets a value indicating whether this type is an array type.
    /// </summary>
    public bool IsArrayType { get; set; }

    /// <summary>
    /// Gets or sets the type of the array items, if this is an array type.
    /// </summary>
    public string? ArrayItemType { get; set; }
}

/// <summary>
/// Represents a property of a complex type in a WSDL document.
/// </summary>
public class WsdlTypeProperty
{
    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of the property.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this property is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this property is a collection.
    /// </summary>
    public bool IsCollection { get; set; }

    /// <summary>
    /// Gets or sets the documentation text for this property, if any.
    /// </summary>
    public string? Documentation { get; set; }
}

/// <summary>
/// Specifies the kind of a WSDL type.
/// </summary>
public enum WsdlTypeKind
{
    /// <summary>
    /// Represents a simple type, such as a string or integer.
    /// </summary>
    Simple,

    /// <summary>
    /// Represents a complex type with properties.
    /// </summary>
    Complex,

    /// <summary>
    /// Represents an enumeration type.
    /// </summary>
    Enum
}
