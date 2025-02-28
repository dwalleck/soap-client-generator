using System;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace SoapClientGenerator.Parser.Tests
{
    public class TestRunner
    {
        public static void Main(string[] args)
        {
            var testClass = typeof(WsdlParserTests);
            var instance = Activator.CreateInstance(testClass);
            
            var results = new System.Text.StringBuilder();
            
            foreach (var method in testClass.GetMethods())
            {
                if (method.GetCustomAttribute<FactAttribute>() != null)
                {
                    try
                    {
                        method.Invoke(instance, null);
                        results.AppendLine($"PASS: {method.Name}");
                    }
                    catch (Exception ex)
                    {
                        results.AppendLine($"FAIL: {method.Name}");
                        results.AppendLine($"  Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }
            
            Console.WriteLine(results.ToString());
        }
    }
}
