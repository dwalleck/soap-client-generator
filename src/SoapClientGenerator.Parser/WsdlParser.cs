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
        var soapActionMap = new Dictionary<string, string>();
        
        // First, build a map of operation names to SOAP actions from the bindings
        foreach (var binding in definitions.Elements(wsdlNs + "binding"))
        {
            foreach (var operation in binding.Elements(wsdlNs + "operation"))
            {
                var operationName = operation.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(operationName))
                    continue;
                
                var soapOperation = operation.Element(soapNs + "operation");
                if (soapOperation != null)
                {
                    var soapAction = soapOperation.Attribute("soapAction")?.Value;
                    if (!string.IsNullOrEmpty(soapAction))
                    {
                        // Handle overloaded operations by checking the input name
                        var input = operation.Element(wsdlNs + "input");
                        var inputName = input?.Attribute("name")?.Value;
                        
                        // For overloaded operations, use the input name as the key
                        if (!string.IsNullOrEmpty(inputName))
                        {
                            soapActionMap[inputName] = soapAction;
                        }
                        else
                        {
                            soapActionMap[operationName] = soapAction;
                        }
                    }
                }
            }
        }
        
        // Now parse the operations from the portTypes
        var portTypes = definitions.Elements(wsdlNs + "portType");
        foreach (var portType in portTypes)
        {
            foreach (var operation in portType.Elements(wsdlNs + "operation"))
            {
                var operationName = operation.Attribute("name")?.Value ?? string.Empty;
                var documentation = operation.Element(wsdlNs + "documentation")?.Value ?? string.Empty;
                var input = operation.Element(wsdlNs + "input");
                var output = operation.Element(wsdlNs + "output");
                
                // Get the input and output names
                var inputName = input?.Attribute("name")?.Value;
                var outputName = output?.Attribute("name")?.Value;
                
                // Determine the actual operation name
                string actualOperationName = operationName;
                if (!string.IsNullOrEmpty(inputName))
                {
                    // For overloaded operations, use the input name as the operation name
                    actualOperationName = inputName;
                }
                
                // Try to find the SOAP action
                string soapAction = string.Empty;
                
                // First try with the input name if available
                if (!string.IsNullOrEmpty(inputName) && soapActionMap.TryGetValue(inputName, out var action))
                {
                    soapAction = action;
                }
                // Then try with the operation name
                else if (soapActionMap.TryGetValue(operationName, out action))
                {
                    soapAction = action;
                }
                // If still not found, try to construct it from the target namespace and operation name
                else
                {
                    var targetNamespace = definitions.Attribute("targetNamespace")?.Value ?? string.Empty;
                    if (!string.IsNullOrEmpty(targetNamespace))
                    {
                        if (!string.IsNullOrEmpty(inputName))
                        {
                            soapAction = $"{targetNamespace.TrimEnd('/')}/{inputName}";
                        }
                        else
                        {
                            soapAction = $"{targetNamespace.TrimEnd('/')}/{operationName}";
                        }
                    }
                }
                
                // Create the operation
                result.Add(new WsdlOperation
                {
                    Name = actualOperationName,
                    Documentation = documentation,
                    SoapAction = soapAction,
                    Input = new WsdlMessage
                    {
                        Name = inputName ?? $"{operationName}Request",
                        Element = input?.Attribute("message")?.Value ?? string.Empty
                    },
                    Output = new WsdlMessage
                    {
                        Name = outputName ?? $"{operationName}Response",
                        Element = output?.Attribute("message")?.Value ?? string.Empty
                    }
                });
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
            var bindingName = binding.Attribute("name")?.Value ?? string.Empty;
            var soapBinding = binding.Element(soapNs + "binding");
            var operations = new Dictionary<string, string>();

            // Parse all operations from the binding
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
                        // Handle overloaded operations with different input/output
                        var inputElement = operation.Element(wsdlNs + "input");
                        var inputName = inputElement?.Attribute("name")?.Value;
                        
                        // If this is an overloaded operation with a specific input name, use that as the key
                        string operationKey = name;
                        if (!string.IsNullOrEmpty(inputName))
                        {
                            operationKey = inputName;
                        }
                        
                        operations[operationKey] = soapAction;
                    }
                }
            }

            result.Add(new WsdlBinding
            {
                Name = bindingName,
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
