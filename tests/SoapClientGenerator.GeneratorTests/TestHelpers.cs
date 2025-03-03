using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SoapClientGenerator.Parser;
using SoapClientGenerator.Parser.Models;

namespace SoapClientGenerator.GeneratorTests;

/// <summary>
/// Helper methods for test operations.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Gets the full path to a test resource file.
    /// </summary>
    /// <param name="relativePath">The relative path to the resource file from the TestResources directory.</param>
    /// <returns>The full path to the resource file.</returns>
    public static string GetTestResourcePath(string relativePath)
    {
        return Path.Combine("TestResources", relativePath);
    }

    /// <summary>
    /// Reads the content of a test resource file.
    /// </summary>
    /// <param name="relativePath">The relative path to the resource file from the TestResources directory.</param>
    /// <returns>The content of the resource file as a string.</returns>
    public static string ReadTestResource(string relativePath)
    {
        var path = GetTestResourcePath(relativePath);
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Creates a temporary directory for testing.
    /// </summary>
    /// <returns>The path to the created directory.</returns>
    public static string CreateTemporaryDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SoapClientGeneratorTests", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    /// <summary>
    /// Deletes a directory and all its contents.
    /// </summary>
    /// <param name="directoryPath">The path to the directory to delete.</param>
    public static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }

    /// <summary>
    /// Parses a WSDL file and returns the definition.
    /// </summary>
    /// <param name="wsdlPath">The path to the WSDL file.</param>
    /// <returns>The parsed WSDL definition.</returns>
    public static WsdlDefinition ParseWsdl(string wsdlPath)
    {
        var parser = new WsdlParser();
        var wsdlContent = File.ReadAllText(wsdlPath);
        return parser.Parse(wsdlContent);
    }

    /// <summary>
    /// Checks if a file exists and contains the specified text.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="text">The text to search for.</param>
    /// <returns>True if the file exists and contains the text, false otherwise.</returns>
    public static bool FileContainsText(string filePath, string text)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        var content = File.ReadAllText(filePath);
        return content.Contains(text);
    }

    /// <summary>
    /// Checks if a directory contains a file with the specified name.
    /// </summary>
    /// <param name="directoryPath">The path to the directory.</param>
    /// <param name="fileName">The name of the file to search for.</param>
    /// <returns>True if the directory contains the file, false otherwise.</returns>
    public static bool DirectoryContainsFile(string directoryPath, string fileName)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        return File.Exists(Path.Combine(directoryPath, fileName));
    }

    /// <summary>
    /// Checks if a directory contains a subdirectory with the specified name.
    /// </summary>
    /// <param name="directoryPath">The path to the directory.</param>
    /// <param name="subdirectoryName">The name of the subdirectory to search for.</param>
    /// <returns>True if the directory contains the subdirectory, false otherwise.</returns>
    public static bool DirectoryContainsSubdirectory(string directoryPath, string subdirectoryName)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        return Directory.Exists(Path.Combine(directoryPath, subdirectoryName));
    }
}
