using System;
using System.Collections.Generic;
using System.Text;
using SoapClientGenerator.Parser.Models;

namespace SoapClientGenerator.Generator.Templates;

/// <summary>
/// Template for generating SOAP client code
/// </summary>
internal static class ClientTemplate
{
    /// <summary>
    /// Generates the client class code
    /// </summary>
    /// <param name="options">Generator options</param>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="operations">List of operations</param>
    /// <returns>The generated client class code</returns>
    public static string GenerateClientClass(SoapClientGeneratorOptions options, string serviceName, IEnumerable<WsdlOperation> operations)
    {
        var sb = new StringBuilder();

        // Add using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Xml.Linq;");
        sb.AppendLine("using System.Xml.Serialization;");
        sb.AppendLine($"using {options.Namespace}.DataContracts;");
        sb.AppendLine($"using {options.Namespace}.ServiceContracts;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {options.Namespace}");
        sb.AppendLine("{");

        // Add XML comments if enabled
        if (options.GenerateXmlComments)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Client for the {serviceName} SOAP service");
            sb.AppendLine("    /// </summary>");
        }

        // Add client class
        sb.AppendLine($"    public class {options.ClientName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly HttpClient _httpClient;");
        sb.AppendLine("        private readonly string _endpoint;");
        sb.AppendLine("        private readonly XNamespace _soapNs = \"http://schemas.xmlsoap.org/soap/envelope/\";");
        sb.AppendLine();

        // Add constructor
        if (options.GenerateXmlComments)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Initializes a new instance of the <see cref=\"{options.ClientName}\"/> class");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"httpClient\">The HTTP client to use for requests</param>");
            sb.AppendLine("        /// <param name=\"endpoint\">The endpoint URL of the SOAP service</param>");
        }
        sb.AppendLine($"        public {options.ClientName}(HttpClient httpClient, string endpoint)");
        sb.AppendLine("        {");
        sb.AppendLine("            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));");
        sb.AppendLine("            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Add properties for auth header credentials
        sb.AppendLine("        private string? _username;");
        sb.AppendLine("        private string? _password;");
        sb.AppendLine("        private bool _useAuthHeader;");
        sb.AppendLine();

        // Add method to set auth credentials
        if (options.GenerateXmlComments)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Sets the authentication credentials for the SOAP service");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"username\">The username for authentication</param>");
            sb.AppendLine("        /// <param name=\"password\">The password for authentication</param>");
        }
        sb.AppendLine("        public void SetAuthCredentials(string username, string password)");
        sb.AppendLine("        {");
        sb.AppendLine("            _username = username;");
        sb.AppendLine("            _password = password;");
        sb.AppendLine("            _useAuthHeader = true;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Add helper method for creating SOAP envelope
        sb.AppendLine("        private XDocument CreateSoapEnvelope(string action, XElement content, bool requiresAuth = false)");
        sb.AppendLine("        {");
        sb.AppendLine("            var headerElement = new XElement(_soapNs + \"Header\");");
        sb.AppendLine();
        sb.AppendLine("            // Add auth header if required and credentials are set");
        sb.AppendLine("            if (requiresAuth && _useAuthHeader)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))");
        sb.AppendLine("                    throw new InvalidOperationException(\"Authentication credentials not set. Call SetAuthCredentials before making authenticated requests.\");");
        sb.AppendLine();
        sb.AppendLine("                var tns = \"http://www.swbc.com/\";");
        sb.AppendLine("                var authHeader = new XElement(XName.Get(\"SWBCAuthHeader\", tns),");
        sb.AppendLine("                    new XElement(XName.Get(\"Username\", tns), _username),");
        sb.AppendLine("                    new XElement(XName.Get(\"Password\", tns), _password)");
        sb.AppendLine("                );");
        sb.AppendLine("                headerElement.Add(authHeader);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return new XDocument(");
        sb.AppendLine("                new XElement(_soapNs + \"Envelope\",");
        sb.AppendLine("                    new XAttribute(XNamespace.Xmlns + \"soap\", _soapNs.NamespaceName),");
        sb.AppendLine("                    headerElement,");
        sb.AppendLine("                    new XElement(_soapNs + \"Body\", content)");
        sb.AppendLine("                )");
        sb.AppendLine("            );");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Add helper method for sending SOAP request
        if (options.GenerateAsyncMethods)
        {
            sb.AppendLine("        private async Task<XDocument> SendSoapRequestAsync(string action, XDocument soapEnvelope, CancellationToken cancellationToken = default)");
            sb.AppendLine("        {");
            sb.AppendLine("            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)");
            sb.AppendLine("            {");
            sb.AppendLine("                Content = new StringContent(soapEnvelope.ToString(), Encoding.UTF8, \"text/xml\")");
            sb.AppendLine("            };");
            sb.AppendLine();
            sb.AppendLine("            request.Headers.Add(\"SOAPAction\", action);");
            sb.AppendLine();
            sb.AppendLine("            var response = await _httpClient.SendAsync(request, cancellationToken);");
            sb.AppendLine("            response.EnsureSuccessStatusCode();");
            sb.AppendLine();
            sb.AppendLine("            var content = await response.Content.ReadAsStringAsync();");
            sb.AppendLine("            return XDocument.Parse(content);");
            sb.AppendLine("        }");
        }
        else
        {
            sb.AppendLine("        private XDocument SendSoapRequest(string action, XDocument soapEnvelope)");
            sb.AppendLine("        {");
            sb.AppendLine("            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)");
            sb.AppendLine("            {");
            sb.AppendLine("                Content = new StringContent(soapEnvelope.ToString(), Encoding.UTF8, \"text/xml\")");
            sb.AppendLine("            };");
            sb.AppendLine();
            sb.AppendLine("            request.Headers.Add(\"SOAPAction\", action);");
            sb.AppendLine();
            sb.AppendLine("            var response = _httpClient.Send(request);");
            sb.AppendLine("            response.EnsureSuccessStatusCode();");
            sb.AppendLine();
            sb.AppendLine("            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();");
            sb.AppendLine("            return XDocument.Parse(content);");
            sb.AppendLine("        }");
        }

        // Add methods for each operation
        sb.AppendLine();
        foreach (var operation in operations)
        {
            // Use the operation name to determine the request and response types
            string operationName = operation.Name;
            string requestType = $"{operationName}";
            string responseType = $"{operationName}Response";

            // Check if this operation requires auth
            bool requiresAuth = operation.AuthHeader != null;

            if (options.GenerateXmlComments)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Calls the {operationName} operation");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        /// <param name=\"request\">The {requestType} request object</param>");
                sb.AppendLine("        /// <param name=\"cancellationToken\">A cancellation token that can be used to cancel the operation</param>");
                sb.AppendLine($"        /// <returns>The {responseType} response from the operation</returns>");
                if (requiresAuth)
                {
                    sb.AppendLine("        /// <remarks>");
                    sb.AppendLine("        /// This operation requires authentication. Call SetAuthCredentials before using this method.");
                    sb.AppendLine("        /// </remarks>");
                }
            }

            if (options.GenerateAsyncMethods)
            {
                sb.AppendLine($"        public async Task<{responseType}> {operationName}Async({requestType} request, CancellationToken cancellationToken = default)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var soapAction = \"{operation.SoapAction}\";");
                sb.AppendLine();
                sb.AppendLine("            // Create request element from the strongly-typed request object");
                sb.AppendLine("            var serializer = new System.Xml.Serialization.XmlSerializer(request.GetType());");
                sb.AppendLine("            var requestXml = new System.IO.StringWriter();");
                sb.AppendLine("            serializer.Serialize(requestXml, request);");
                sb.AppendLine("            var requestElement = XElement.Parse(requestXml.ToString());");
                sb.AppendLine();
                sb.AppendLine($"            var soapEnvelope = CreateSoapEnvelope(soapAction, requestElement, {(requiresAuth ? "true" : "false")});");
                sb.AppendLine();
                sb.AppendLine("            var responseDocument = await SendSoapRequestAsync(soapAction, soapEnvelope, cancellationToken);");
                sb.AppendLine("            var responseBody = responseDocument.Descendants(_soapNs + \"Body\").FirstOrDefault();");
                sb.AppendLine();
                sb.AppendLine("            // Deserialize the response into a strongly-typed object");
                sb.AppendLine($"            var responseSerializer = new System.Xml.Serialization.XmlSerializer(typeof({responseType}));");
                sb.AppendLine("            var responseReader = new System.IO.StringReader(responseBody.ToString());");
                sb.AppendLine($"            return ({responseType})responseSerializer.Deserialize(responseReader);");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine($"        public {responseType} {operationName}({requestType} request)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var soapAction = \"{operation.SoapAction}\";");
                sb.AppendLine();
                sb.AppendLine("            // Create request element from the strongly-typed request object");
                sb.AppendLine("            var serializer = new System.Xml.Serialization.XmlSerializer(request.GetType());");
                sb.AppendLine("            var requestXml = new System.IO.StringWriter();");
                sb.AppendLine("            serializer.Serialize(requestXml, request);");
                sb.AppendLine("            var requestElement = XElement.Parse(requestXml.ToString());");
                sb.AppendLine();
                sb.AppendLine($"            var soapEnvelope = CreateSoapEnvelope(soapAction, requestElement, {(requiresAuth ? "true" : "false")});");
                sb.AppendLine();
                sb.AppendLine("            var responseDocument = SendSoapRequest(soapAction, soapEnvelope);");
                sb.AppendLine("            var responseBody = responseDocument.Descendants(_soapNs + \"Body\").FirstOrDefault();");
                sb.AppendLine();
                sb.AppendLine("            // Deserialize the response into a strongly-typed object");
                sb.AppendLine($"            var responseSerializer = new System.Xml.Serialization.XmlSerializer(typeof({responseType}));");
                sb.AppendLine("            var responseReader = new System.IO.StringReader(responseBody.ToString());");
                sb.AppendLine($"            return ({responseType})responseSerializer.Deserialize(responseReader);");
                sb.AppendLine("        }");
            }

            sb.AppendLine();
        }

        // Close class and namespace
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a data contract class
    /// </summary>
    /// <param name="options">Generator options</param>
    /// <param name="typeName">Name of the type</param>
    /// <param name="properties">Dictionary of property names, types, and whether they are required</param>
    /// <returns>The generated data contract class code</returns>
    public static string GenerateDataContract(SoapClientGeneratorOptions options, string typeName, Dictionary<string, (string Type, bool IsRequired)> properties)
    {
        var sb = new StringBuilder();

        // Add using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Xml.Serialization;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {options.Namespace}.DataContracts");
        sb.AppendLine("{");

        // Add XML comments if enabled
        if (options.GenerateXmlComments)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Data contract for {typeName}");
            sb.AppendLine("    /// </summary>");
        }

        // Add class
        sb.AppendLine($"    public class {typeName}");
        sb.AppendLine("    {");

        // Add properties
        foreach (var property in properties)
        {
            string propertyType = property.Value.Type;
            bool isRequired = property.Value.IsRequired;
            string propertyName = property.Key;

            // Check if property name is a C# keyword
            if (IsCSharpKeyword(propertyName))
            {
                propertyName = "@" + propertyName;
            }

            // Make non-required properties nullable if they're value types
            if (!isRequired && !propertyType.EndsWith("?") && !propertyType.StartsWith("List<") &&
                !propertyType.Equals("string") && !propertyType.Equals("object") && !propertyType.EndsWith("[]"))
            {
                propertyType = $"{propertyType}?";
            }

            if (options.GenerateXmlComments)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Gets or sets the {property.Key}");
                if (!isRequired)
                {
                    sb.AppendLine("        /// <para>This property is optional.</para>");
                }
                sb.AppendLine("        /// </summary>");
            }

            // Add XML serialization attributes
            if (!isRequired)
            {
                sb.AppendLine($"        [XmlElement(ElementName = \"{property.Key}\", IsNullable = true)]");
            }
            else
            {
                sb.AppendLine($"        [XmlElement(ElementName = \"{property.Key}\")]");
            }

            sb.AppendLine($"        public {propertyType} {propertyName} {{ get; set; }}");
            sb.AppendLine();
        }

        // Close class and namespace
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Checks if a string is a C# keyword
    /// </summary>
    /// <param name="word">The word to check</param>
    /// <returns>True if the word is a C# keyword, false otherwise</returns>
    private static bool IsCSharpKeyword(string word)
    {
        // List of C# keywords
        var keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while"
        };

        return keywords.Contains(word);
    }

    /// <summary>
    /// Generates an enum type
    /// </summary>
    /// <param name="options">Generator options</param>
    /// <param name="enumName">Name of the enum</param>
    /// <param name="values">List of enum values</param>
    /// <returns>The generated enum code</returns>
    public static string GenerateEnum(SoapClientGeneratorOptions options, string enumName, IEnumerable<string> values)
    {
        var sb = new StringBuilder();

        // Add using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Xml.Serialization;");
        sb.AppendLine();

        // Add namespace
        sb.AppendLine($"namespace {options.Namespace}.DataContracts");
        sb.AppendLine("{");

        // Add XML comments if enabled
        if (options.GenerateXmlComments)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Enum for {enumName}");
            sb.AppendLine("    /// </summary>");
        }

        // Add enum
        sb.AppendLine($"    public enum {enumName}");
        sb.AppendLine("    {");

        // Add values
        var valuesList = new List<string>(values);
        for (int i = 0; i < valuesList.Count; i++)
        {
            var value = valuesList[i];
            if (options.GenerateXmlComments)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {value}");
                sb.AppendLine("        /// </summary>");
            }
            sb.AppendLine($"        [XmlEnum(Name = \"{value}\")]");
            sb.Append($"        {value}");

            if (i < valuesList.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        // Close enum and namespace
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
