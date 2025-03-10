using System.Xml.Linq;
using SoapClientGenerator.Parser.Models;

namespace SoapClientGenerator.Parser;

/// <summary>
/// Parser for WSDL (Web Services Description Language) documents.
/// </summary>
public class WsdlParser
{
    private readonly XNamespace wsdlNs = "http://schemas.xmlsoap.org/wsdl/";
    private readonly XNamespace soapNs = "http://schemas.xmlsoap.org/wsdl/soap/";
    private readonly XNamespace xsdNs = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// Parses a WSDL document from its XML content.
    /// </summary>
    /// <param name="wsdlContent">The XML content of the WSDL document.</param>
    /// <returns>A <see cref="WsdlDefinition"/> object representing the parsed WSDL document.</returns>
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

    /// <summary>
    /// Parses the namespace declarations from the WSDL definitions element.
    /// </summary>
    /// <param name="definitions">The WSDL definitions element.</param>
    /// <returns>A dictionary mapping namespace prefixes to their URIs.</returns>
    private Dictionary<string, string> ParseNamespaces(XElement definitions)
    {
        return definitions.Attributes()
            .Where(a => a.IsNamespaceDeclaration)
            .ToDictionary(
                a => a.Name.LocalName,
                a => a.Value
            );
    }

    /// <summary>
    /// Parses the types section of a WSDL document.
    /// </summary>
    /// <param name="types">The types element from the WSDL document.</param>
    /// <returns>A list of <see cref="WsdlType"/> objects representing the types defined in the WSDL.</returns>
    private List<WsdlType> ParseTypes(XElement? types)
    {
        var result = new List<WsdlType>();
        if (types == null) return result;

        var schemas = types.Elements(xsdNs + "schema");
        foreach (var schema in schemas)
        {
            var targetNamespace = schema.Attribute("targetNamespace")?.Value ?? string.Empty;
            var elementFormDefault = schema.Attribute("elementFormDefault")?.Value;
            bool isElementFormQualified = elementFormDefault == "qualified";

            // Extract namespace prefix from targetNamespace
            string? namespacePrefix = null;
            if (isElementFormQualified && !string.IsNullOrEmpty(targetNamespace))
            {
                // Extract the domain part from the URL
                Uri uri;
                if (Uri.TryCreate(targetNamespace, UriKind.Absolute, out uri))
                {
                    // Get the host part and extract the domain name
                    string host = uri.Host;
                    string[] parts = host.Split('.');

                    // Find the main domain name part (usually the second-to-last part)
                    if (parts.Length >= 2)
                    {
                        // Skip common subdomains like "www", "api", etc.
                        int domainPartIndex = 0;
                        if (parts.Length > 2 && (parts[0] == "www" || parts[0] == "api" || parts[0] == "services"))
                        {
                            domainPartIndex = 1;
                        }

                        // Use the domain name (e.g., "testcompany" from "www.testcompany.com")
                        namespacePrefix = parts[domainPartIndex];
                    }
                }
            }

            // Parse complex types
            foreach (var complexType in schema.Elements(xsdNs + "complexType"))
            {
                var type = ParseComplexType(complexType, targetNamespace);
                type.ElementFormQualified = isElementFormQualified;
                type.NamespacePrefix = namespacePrefix;
                result.Add(type);
            }

            // Parse simple types
            foreach (var simpleType in schema.Elements(xsdNs + "simpleType"))
            {
                var type = ParseSimpleType(simpleType, targetNamespace);
                type.ElementFormQualified = isElementFormQualified;
                type.NamespacePrefix = namespacePrefix;
                result.Add(type);
            }

            // Parse elements that have complex types inline
            foreach (var element in schema.Elements(xsdNs + "element"))
            {
                var complexType = element.Element(xsdNs + "complexType");
                if (complexType != null)
                {
                    var type = ParseComplexType(complexType, targetNamespace, element.Attribute("name")?.Value);
                    type.ElementFormQualified = isElementFormQualified;
                    type.NamespacePrefix = namespacePrefix;
                    result.Add(type);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Parses a complex type element from a WSDL document.
    /// </summary>
    /// <param name="complexType">The complex type element to parse.</param>
    /// <param name="targetNamespace">The target namespace of the schema containing this type.</param>
    /// <param name="elementName">The name of the element if this is an inline complex type, or null if it's a named complex type.</param>
    /// <returns>A <see cref="WsdlType"/> object representing the complex type.</returns>
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

        // Check if this is an array type (has a single property with maxOccurs="unbounded")
        bool isArrayType = false;
        string? itemType = null;
        if (properties.Count == 1 && properties[0].IsCollection)
        {
            isArrayType = true;
            itemType = properties[0].Type;
        }

        return new WsdlType
        {
            Name = name,
            Namespace = targetNamespace,
            Kind = WsdlTypeKind.Complex,
            Properties = properties,
            IsArrayType = isArrayType,
            ArrayItemType = itemType
        };
    }

    /// <summary>
    /// Parses a simple type element from a WSDL document.
    /// </summary>
    /// <param name="simpleType">The simple type element to parse.</param>
    /// <param name="targetNamespace">The target namespace of the schema containing this type.</param>
    /// <returns>A <see cref="WsdlType"/> object representing the simple type.</returns>
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

    /// <summary>
    /// Parses the operations from a WSDL document.
    /// </summary>
    /// <param name="definitions">The WSDL definitions element.</param>
    /// <returns>A list of <see cref="WsdlOperation"/> objects representing the operations defined in the WSDL.</returns>
    private List<WsdlOperation> ParseOperations(XElement definitions)
    {
        var result = new List<WsdlOperation>();
        var soapActionMap = new Dictionary<string, string>();
        var authHeaderMap = new Dictionary<string, WsdlAuthHeader>();

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

        // Find auth headers in messages
        var authHeaders = new Dictionary<string, (string Element, string TypeName)>();
        foreach (var message in definitions.Elements(wsdlNs + "message"))
        {
            var messageName = message.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(messageName))
                continue;

            var part = message.Element(wsdlNs + "part");
            if (part != null)
            {
                var element = part.Attribute("element")?.Value;
                if (!string.IsNullOrEmpty(element))
                {
                    // Extract the local name from the qualified name
                    string typeName = element;
                    if (element.Contains(":"))
                    {
                        typeName = element.Split(':')[1];
                    }

                    // Check if this is an auth header (either by name convention or by examining the element name)
                    if (messageName.EndsWith("SWBCAuthHeader") ||
                        messageName.EndsWith("AuthHeader") ||
                        typeName.EndsWith("AuthHeader") ||
                        typeName.Contains("Auth"))
                    {
                        authHeaders[messageName] = (element, typeName);
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
                var wsdlOperation = new WsdlOperation
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
                };

                // Look for auth headers in the binding operations
                foreach (var binding in definitions.Elements(wsdlNs + "binding"))
                {
                    var bindingOperation = binding.Elements(wsdlNs + "operation")
                        .FirstOrDefault(op => op.Attribute("name")?.Value == operationName);

                    if (bindingOperation != null)
                    {
                        var bindingInput = bindingOperation.Element(wsdlNs + "input");
                        if (bindingInput != null)
                        {
                            var soapHeader = bindingInput.Element(soapNs + "header");
                            if (soapHeader != null)
                            {
                                var headerMessage = soapHeader.Attribute("message")?.Value;
                                if (!string.IsNullOrEmpty(headerMessage) && authHeaders.TryGetValue(headerMessage.Split(':').Last(), out var headerInfo))
                                {
                                    wsdlOperation.AuthHeader = new WsdlAuthHeader
                                    {
                                        Name = headerMessage.Split(':').Last(),
                                        Element = headerInfo.Element,
                                        TypeName = headerInfo.TypeName
                                    };
                                }
                            }
                        }
                    }
                }

                result.Add(wsdlOperation);
            }
        }

        return result;
    }

    /// <summary>
    /// Parses the bindings from a WSDL document.
    /// </summary>
    /// <param name="definitions">The WSDL definitions element.</param>
    /// <returns>A list of <see cref="WsdlBinding"/> objects representing the bindings defined in the WSDL.</returns>
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

    /// <summary>
    /// Parses the services from a WSDL document.
    /// </summary>
    /// <param name="definitions">The WSDL definitions element.</param>
    /// <returns>A list of <see cref="WsdlService"/> objects representing the services defined in the WSDL.</returns>
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
