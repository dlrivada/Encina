using System.Dynamic;
using System.Text.Json;
using Encina.Security.ABAC.EEL;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.EEL;

/// <summary>
/// Data-driven conformance tests for the EEL (Encina Expression Language) compiler.
/// Loads test cases from JSON files in TestData/ and verifies each expression
/// evaluates to the expected result or produces the expected error.
/// </summary>
public sealed class EELConformanceTests : IDisposable
{
    private readonly EELCompiler _compiler = new();

    public void Dispose() => _compiler.Dispose();

    #region Success Tests

    [Theory]
    [MemberData(nameof(GetSuccessTestCases))]
    public async Task EEL_Conformance_SuccessCase(string category, string testName, string expression, bool expected, JsonElement globalsElement)
    {
        var globals = BuildGlobals(globalsElement);

        var result = await _compiler.EvaluateAsync(expression, globals);

        result.IsRight.ShouldBeTrue(
            $"[{category}/{testName}] expression '{expression}' should compile and evaluate successfully");

        var actual = result.Match(Left: _ => !expected, Right: v => v);
        actual.ShouldBe(expected,
            $"[{category}/{testName}] expression '{expression}' should evaluate to {expected}");
    }

    #endregion

    #region Error Tests

    [Theory]
    [MemberData(nameof(GetErrorTestCases))]
    public async Task EEL_Conformance_ErrorCase(string category, string testName, string expression, string expectedError, JsonElement globalsElement)
    {
        var globals = BuildGlobals(globalsElement);

        var result = await _compiler.EvaluateAsync(expression, globals);

        result.IsLeft.ShouldBeTrue(
            $"[{category}/{testName}] expression '{expression}' should produce an error");

        var errorCode = result.Match(
            Left: error => error.GetCode().IfNone(""),
            Right: _ => "");
        errorCode.ShouldBe(expectedError,
            $"[{category}/{testName}] expression '{expression}' should produce error code '{expectedError}'");
    }

    #endregion

    #region Test Data Providers

    public static IEnumerable<object[]> GetSuccessTestCases()
    {
        var testDataPath = GetTestDataPath();
        var files = Directory.GetFiles(testDataPath, "eel-conformance-*.json");

        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var category = root.GetProperty("category").GetString()!;

            foreach (var test in root.GetProperty("tests").EnumerateArray())
            {
                // Skip error test cases (they have "expectedError" instead of "expected")
                if (test.TryGetProperty("expectedError", out _))
                {
                    continue;
                }

                var name = test.GetProperty("name").GetString()!;
                var expression = test.GetProperty("expression").GetString()!;
                var expected = test.GetProperty("expected").GetBoolean();
                var globals = test.GetProperty("globals");

                yield return [category, name, expression, expected, globals];
            }
        }
    }

    public static IEnumerable<object[]> GetErrorTestCases()
    {
        var testDataPath = GetTestDataPath();
        var files = Directory.GetFiles(testDataPath, "eel-conformance-*.json");

        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var category = root.GetProperty("category").GetString()!;

            foreach (var test in root.GetProperty("tests").EnumerateArray())
            {
                // Only include error test cases
                if (!test.TryGetProperty("expectedError", out var errorProp))
                {
                    continue;
                }

                var name = test.GetProperty("name").GetString()!;
                var expression = test.GetProperty("expression").GetString()!;
                var expectedError = errorProp.GetString()!;
                var globals = test.GetProperty("globals");

                yield return [category, name, expression, expectedError, globals];
            }
        }
    }

    #endregion

    #region Helpers

    private static string GetTestDataPath()
    {
        // Navigate from the bin output directory to the source test data directory
        var baseDir = AppContext.BaseDirectory;
        var testDataPath = Path.Combine(baseDir, "Security", "ABAC", "EEL", "TestData");

        if (!Directory.Exists(testDataPath))
        {
            // Fallback: navigate from project output to source location
            var projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            testDataPath = Path.Combine(projectDir, "Encina.UnitTests", "Security", "ABAC", "EEL", "TestData");
        }

        return testDataPath;
    }

    private static EELGlobals BuildGlobals(JsonElement globalsElement)
    {
        var globals = new EELGlobals
        {
            user = BuildExpando(globalsElement.GetProperty("user")),
            resource = BuildExpando(globalsElement.GetProperty("resource")),
            environment = BuildExpando(globalsElement.GetProperty("environment")),
            action = BuildExpando(globalsElement.GetProperty("action"))
        };

        return globals;
    }

    private static ExpandoObject BuildExpando(JsonElement element)
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = ConvertJsonValue(property.Value);
            }
        }

        return expando;
    }

    private static object? ConvertJsonValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonValue)
                .ToList(),
            JsonValueKind.Object => BuildExpando(element),
            _ => element.GetRawText()
        };

    #endregion
}
