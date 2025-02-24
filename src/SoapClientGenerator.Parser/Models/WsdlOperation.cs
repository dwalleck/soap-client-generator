namespace SoapClientGenerator.Parser.Models;

public class WsdlOperation
{
    public required string Name { get; set; }
    public required string SoapAction { get; set; }
    public required string Documentation { get; set; }
    public required WsdlMessage Input { get; set; }
    public required WsdlMessage Output { get; set; }
    public ICollection<WsdlMessage> Headers { get; set; } = new List<WsdlMessage>();
}

public class WsdlMessage
{
    public required string Name { get; set; }
    public required string Element { get; set; }
}
