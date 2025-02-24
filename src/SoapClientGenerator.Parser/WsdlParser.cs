using System.Xml.Linq;
using SoapClientGenerator.Parser.Models;

namespace SoapClientGenerator.Parser;

public class WsdlParser
{
    private readonly XNamespace wsdlNs = "http://schemas.xmlsoap.org/wsdl/";
    private readonly XNamespace soapNs = "http://schemas.xmlsoap.org/wsdl/soap/";
    private readonly XNamespace xsdNs = "http://www.w3.org/2001/XMLSchema";

    public WsdlDefinition Parse(string wsdlContent)
    {
        var doc = XDocument.Parse(wsdlContent);
        var definitions = doc.Root!;

        var wsdl = new WsdlDefinition
        {
            TargetNamespace = definitions.Attribute("targetNamespace")?.Value ?? string.Empty,
            Namespaces = ParseNamespaces(definitions),
            Types = ParseTypes(definitions.Element(wsdlNs + "types")),
            Operations = ParseOperations(definitions),
            Bindings = ParseBindings(definitions),
            Services = ParseServices(definitions)
        };

        return wsdl;
    }

    private Dictionary<string, string> ParseNamespaces(XElement definitions)
    {
        return definitions.Attributes()
            .Where(a => a.IsNamespaceDeclaration)
            .ToDictionary(
                a => a.Name.LocalName,
                a => a.Value
            );
    }

    private List<WsdlType> ParseTypes(XElement? types)
    {
        var result = new List<WsdlType>();
        if (types == null) return result;

        var schemas = types.Elements(xsdNs + "schema");
        foreach (var schema in schemas)
        {
            var targetNamespace = schema.Attribute("targetNamespace")?.Value ?? string.Empty;
            
            // Parse complex types
            foreach (var complexType in schema.Elements(xsdNs + "complexType"))
            {
                result.Add(ParseComplexType(complexType, targetNamespace));
            }

            // Parse simple types
            foreach (var simpleType in schema.Elements(xsdNs + "simpleType"))
            {
                result.Add(ParseSimpleType(simpleType, targetNamespace));
            }

            // Parse elements that have complex types inline
            foreach (var element in schema.Elements(xsdNs + "element"))
            {
                var complexType = element.Element(xsdNs + "complexType");
                if (complexType != null)
                {
                    result.Add(ParseComplexType(complexType, targetNamespace, element.Attribute("name")?.Value));
                }
            }
        }

        return result;
    }

    private WsdlType ParseComplexType(XElement complexType, string targetNamespace, string? elementName = null)
    {
        var name = elementName ?? complexType.Attribute("name")?.Value ?? string.Empty;
        var properties = new List<WsdlTypeProperty>();

        var sequence = complexType.Descendants(xsdNs + "sequence").FirstOrDefault();
        if (sequence != null)
        {
            foreach (var element in sequence.Elements(xsdNs + "element"))
            {
                properties.Add(new WsdlTypeProperty
                {
                    Name = element.Attribute("name")?.Value ?? string.Empty,
                    Type = element.Attribute("type")?.Value ?? string.Empty,
                    IsRequired = element.Attribute("minOccurs")?.Value != "0",
                    IsCollection = element.Attribute("maxOccurs")?.Value == "unbounded"
                });
            }
        }

        return new WsdlType
        {
            Name = name,
            Namespace = targetNamespace,
            Kind = WsdlTypeKind.Complex,
            Properties = properties
        };
    }

    private WsdlType ParseSimpleType(XElement simpleType, string targetNamespace)
    {
        var name = simpleType.Attribute("name")?.Value ?? string.Empty;
        var restriction = simpleType.Element(xsdNs + "restriction");
        var enumValues = new List<string>();

        if (restriction != null)
        {
            var enums = restriction.Elements(xsdNs + "enumeration")
                .Select(e => e.Attribute("value")?.Value ?? string.Empty)
                .ToList();

            if (enums.Any())
            {
                return new WsdlType
                {
                    Name = name,
                    Namespace = targetNamespace,
                    Kind = WsdlTypeKind.Enum,
                    IsEnum = true,
                    EnumValues = enums
                };
            }
        }

        return new WsdlType
        {
            Name = name,
            Namespace = targetNamespace,
            Kind = WsdlTypeKind.Simple,
            BaseType = restriction?.Attribute("base")?.Value
        };
    }

    private List<WsdlOperation> ParseOperations(XElement definitions)
    {
        var result = new List<WsdlOperation>();
        var portTypes = definitions.Elements(wsdlNs + "portType");

        foreach (var portType in portTypes)
        {
            foreach (var operation in portType.Elements(wsdlNs + "operation"))
            {
                var baseOperationName = operation.Attribute("name")?.Value ?? string.Empty;
                var documentation = operation.Element(wsdlNs + "documentation")?.Value ?? string.Empty;
                var input = operation.Element(wsdlNs + "input");
                var output = operation.Element(wsdlNs + "output");

                // Get the operation name from the binding that matches this operation's input message
                var inputMessageRef = input?.Attribute("message")?.Value ?? string.Empty;
                var bindingOperations = definitions.Elements(wsdlNs + "binding")
                    .Elements(wsdlNs + "operation")
                    .Where(o => {
                        var opName = o.Attribute("name")?.Value;
                        return opName == baseOperationName;
                    })
                    .ToList();

                foreach (var bindingOp in bindingOperations)
                {
                    var soapAction = bindingOp.Element(soapNs + "operation")?.Attribute("soapAction")?.Value ?? string.Empty;
                    var operationName = bindingOp.Attribute("name")?.Value ?? string.Empty;
                    
                    result.Add(new WsdlOperation
                    {
                        Name = operationName,
                        Documentation = documentation,
                        SoapAction = soapAction,
                        Input = new WsdlMessage
                        {
                            Name = input?.Attribute("name")?.Value ?? $"{operationName}Request",
                            Element = inputMessageRef
                        },
                        Output = new WsdlMessage
                        {
                            Name = output?.Attribute("name")?.Value ?? $"{operationName}Response",
                            Element = output?.Attribute("message")?.Value ?? string.Empty
                        }
                    });
                }
            }
        }

        return result;
    }

    private List<WsdlBinding> ParseBindings(XElement definitions)
    {
        var result = new List<WsdlBinding>();
        var bindings = definitions.Elements(wsdlNs + "binding");

        foreach (var binding in bindings)
        {
            var soapBinding = binding.Element(soapNs + "binding");
            var operations = new Dictionary<string, string>();

            var bindingOperations = binding.Elements(wsdlNs + "operation")
                .Where(op => !string.IsNullOrEmpty(op.Attribute("name")?.Value))
                .ToList();

            foreach (var operation in bindingOperations)
            {
                var name = operation.Attribute("name")!.Value;
                var soapOperation = operation.Element(soapNs + "operation");
                if (soapOperation != null)
                {
                    var soapAction = soapOperation.Attribute("soapAction")?.Value ?? string.Empty;
                    if (!string.IsNullOrEmpty(soapAction))
                    {
                        operations[name] = soapAction;
                    }
                }
            }

            result.Add(new WsdlBinding
            {
                Name = binding.Attribute("name")?.Value ?? string.Empty,
                Type = binding.Attribute("type")?.Value ?? string.Empty,
                Transport = soapBinding?.Attribute("transport")?.Value ?? string.Empty,
                OperationSoapActions = operations
            });
        }

        return result;
    }

    private List<WsdlService> ParseServices(XElement definitions)
    {
        var result = new List<WsdlService>();
        var services = definitions.Elements(wsdlNs + "service");

        foreach (var service in services)
        {
            var ports = new List<WsdlPort>();
            foreach (var port in service.Elements(wsdlNs + "port"))
            {
                var address = port.Element(soapNs + "address");
                ports.Add(new WsdlPort
                {
                    Name = port.Attribute("name")?.Value ?? string.Empty,
                    Binding = port.Attribute("binding")?.Value ?? string.Empty,
                    Location = address?.Attribute("location")?.Value ?? string.Empty
                });
            }

            result.Add(new WsdlService
            {
                Name = service.Attribute("name")?.Value ?? string.Empty,
                Ports = ports
            });
        }

        return result;
    }
}
