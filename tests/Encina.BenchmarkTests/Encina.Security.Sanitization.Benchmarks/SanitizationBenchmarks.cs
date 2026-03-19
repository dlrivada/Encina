using BenchmarkDotNet.Jobs;
using Encina.Security.Sanitization.Encoders;
using Microsoft.Extensions.Options;

namespace Encina.Security.Sanitization.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="DefaultSanitizer"/> covering HTML, SQL, Shell, JSON, and XML sanitization.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class SanitizerBenchmarks
{
    private DefaultSanitizer _sanitizer = null!;

    private string _cleanInput = null!;
    private string _maliciousHtml = null!;
    private string _complexDocument = null!;
    private string _simpleSqlInput = null!;
    private string _sqlInjectionAttempt = null!;
    private string _simpleShellInput = null!;
    private string _shellInjectionAttempt = null!;
    private string _simpleJsonInput = null!;
    private string _simpleXmlInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = Options.Create(new SanitizationOptions());
        _sanitizer = new DefaultSanitizer(options);

        _cleanInput = "This is a safe text with no HTML tags at all.";
        _maliciousHtml = "<script>alert('xss')</script><b>text</b>";
        _complexDocument = string.Concat(
            "<div class=\"container\"><h1>Title</h1>",
            "<p>Some paragraph with <a href=\"https://example.com\">a link</a> and <strong>bold text</strong>.</p>",
            "<script>document.cookie</script>",
            "<img src=\"x\" onerror=\"alert(1)\">",
            "<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>",
            "<p>Another paragraph with <em>emphasis</em> and <code>inline code</code>.</p>",
            "<table><tr><td>Cell 1</td><td>Cell 2</td></tr></table>",
            "<iframe src=\"https://evil.com\"></iframe>",
            "<p style=\"color:red;background:url(javascript:alert(1))\">Styled text</p>",
            "</div>");

        _simpleSqlInput = "John O'Brien";
        _sqlInjectionAttempt = "'; DROP TABLE users;--";
        _simpleShellInput = "hello-world";
        _shellInjectionAttempt = "; rm -rf /";
        _simpleJsonInput = "Hello \"world\" with\nnewlines";
        _simpleXmlInput = "Price < 100 & quantity > 0";
    }

    [Benchmark(Baseline = true)]
    public string SanitizeHtml_CleanInput() => _sanitizer.SanitizeHtml(_cleanInput);

    [Benchmark]
    public string SanitizeHtml_MaliciousInput() => _sanitizer.SanitizeHtml(_maliciousHtml);

    [Benchmark]
    public string SanitizeHtml_ComplexDocument() => _sanitizer.SanitizeHtml(_complexDocument);

    [Benchmark]
    public string SanitizeForSql_SimpleInput() => _sanitizer.SanitizeForSql(_simpleSqlInput);

    [Benchmark]
    public string SanitizeForSql_InjectionAttempt() => _sanitizer.SanitizeForSql(_sqlInjectionAttempt);

    [Benchmark]
    public string SanitizeForShell_SimpleInput() => _sanitizer.SanitizeForShell(_simpleShellInput);

    [Benchmark]
    public string SanitizeForShell_InjectionAttempt() => _sanitizer.SanitizeForShell(_shellInjectionAttempt);

    [Benchmark]
    public string SanitizeForJson_SimpleInput() => _sanitizer.SanitizeForJson(_simpleJsonInput);

    [Benchmark]
    public string SanitizeForXml_SimpleInput() => _sanitizer.SanitizeForXml(_simpleXmlInput);
}

/// <summary>
/// Benchmarks for <see cref="DefaultOutputEncoder"/> covering HTML, JavaScript, URL, and CSS encoding.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class OutputEncoderBenchmarks
{
    private DefaultOutputEncoder _encoder = null!;

    private string _safeText = null!;
    private string _htmlSpecialChars = null!;
    private string _jsSpecialChars = null!;
    private string _urlSpecialChars = null!;
    private string _cssSpecialChars = null!;

    [GlobalSetup]
    public void Setup()
    {
        _encoder = new DefaultOutputEncoder();

        _safeText = "This is a safe text with no special characters";
        _htmlSpecialChars = "<script>\"test\" & 'data'</script>";
        _jsSpecialChars = "alert('xss');\nvar x = \"test\\\\ value\";";
        _urlSpecialChars = "hello world/path?query=value&other=<>";
        _cssSpecialChars = "expression(alert('xss')); color: red";
    }

    [Benchmark(Baseline = true)]
    public string EncodeForHtml_SafeText() => _encoder.EncodeForHtml(_safeText);

    [Benchmark]
    public string EncodeForHtml_SpecialChars() => _encoder.EncodeForHtml(_htmlSpecialChars);

    [Benchmark]
    public string EncodeForJavaScript_SafeText() => _encoder.EncodeForJavaScript(_safeText);

    [Benchmark]
    public string EncodeForJavaScript_SpecialChars() => _encoder.EncodeForJavaScript(_jsSpecialChars);

    [Benchmark]
    public string EncodeForUrl_SafeText() => _encoder.EncodeForUrl(_safeText);

    [Benchmark]
    public string EncodeForUrl_SpecialChars() => _encoder.EncodeForUrl(_urlSpecialChars);

    [Benchmark]
    public string EncodeForCss_SafeText() => _encoder.EncodeForCss(_safeText);

    [Benchmark]
    public string EncodeForCss_SpecialChars() => _encoder.EncodeForCss(_cssSpecialChars);
}
