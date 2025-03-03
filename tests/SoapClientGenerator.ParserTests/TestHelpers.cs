using System.IO;
using System.Reflection;

namespace SoapClientGenerator.ParserTests;

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
    /// Creates a temporary WSDL file with the specified content for testing.
    /// </summary>
    /// <param name="wsdlContent">The WSDL content to write to the file.</param>
    /// <param name="fileName">The name of the file to create.</param>
    /// <returns>The full path to the created file.</returns>
    public static string CreateTemporaryWsdlFile(string wsdlContent, string fileName)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SoapClientGeneratorTests");
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, fileName);
        File.WriteAllText(filePath, wsdlContent);

        return filePath;
    }

    /// <summary>
    /// Deletes a temporary file created for testing.
    /// </summary>
    /// <param name="filePath">The path to the file to delete.</param>
    public static void DeleteTemporaryFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
