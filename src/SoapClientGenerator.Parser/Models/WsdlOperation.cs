namespace SoapClientGenerator.Parser.Models;

/// <summary>
/// Represents an operation defined in a WSDL document.
/// </summary>
public class WsdlOperation
{
    /// <summary>
    /// Gets or sets the name of the operation.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the SOAP action URI for this operation.
    /// </summary>
    public required string SoapAction { get; set; }

    /// <summary>
    /// Gets or sets the documentation text for this operation.
    /// </summary>
    public required string Documentation { get; set; }

    /// <summary>
    /// Gets or sets the input message for this operation.
    /// </summary>
    public required WsdlMessage Input { get; set; }

    /// <summary>
    /// Gets or sets the output message for this operation.
    /// </summary>
    public required WsdlMessage Output { get; set; }

    /// <summary>
    /// Gets or sets the collection of header messages for this operation.
    /// </summary>
    public ICollection<WsdlMessage> Headers { get; set; } = new List<WsdlMessage>();
}

/// <summary>
/// Represents a message in a WSDL document, which defines the data being exchanged in an operation.
/// </summary>
public class WsdlMessage
{
    /// <summary>
    /// Gets or sets the name of the message.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the XML element associated with this message.
    /// </summary>
    public required string Element { get; set; }
}
