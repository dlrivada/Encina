using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

using Encina.Security.ABAC;
using Encina.Security.ABAC.EEL;
using Encina.Security.ABAC.Evaluation;
using Encina.Security.ABAC.Persistence;
using Encina.Security.ABAC.Persistence.Xacml;

using Microsoft.Extensions.Logging.Testing;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// No ABAC code path puts an exception message into an error built for callers, an activity status
/// or tag, or a log record (#1993, #1700; AGENTS.md section 3). Each test throws or provokes an
/// exception whose message carries a secret marker and proves the marker reaches none of them. The
/// parser tests first prove the marker really is in the raw exception message, so they stay falsifiable.
/// </summary>
[Collection(ABACActivityListenerIsolation.Name)]
public sealed class ExceptionMessageRedactionTests
{
    private const string Sentinel = "SENTINEL-secret-marker";
    private const string XacmlNamespace = "urn:oasis:names:tc:xacml:3.0:core:schema:wd-17";

    private readonly FakeLogger<XacmlXmlPolicySerializer> _logger = new();

    private static void ShouldNotLeak(EncinaError error)
    {
        error.Message.ShouldNotContain(Sentinel);
        error.GetDetails().Values.ShouldAllBe(v => v == null || !v.ToString()!.Contains(Sentinel, StringComparison.Ordinal));
    }

    private static EncinaError LeftOf<T>(LanguageExt.Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        return result.Match(Left: e => e, Right: _ => throw new InvalidOperationException("expected Left"));
    }

    private static ActivityListener Listen(ConcurrentQueue<Activity> stopped)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Security.ABAC",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Enqueue
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static IEnumerable<string?> ErrorDescriptions(IEnumerable<Activity> activities) =>
        activities.Where(a => a.Status == ActivityStatusCode.Error).Select(a => a.StatusDescription).ToList();

    private static void ShouldNotLeak(IEnumerable<Activity> activities)
    {
        foreach (var activity in activities)
        {
            (activity.StatusDescription ?? string.Empty).ShouldNotContain(Sentinel);
            activity.TagObjects.ShouldAllBe(t => t.Value == null || !t.Value.ToString()!.Contains(Sentinel, StringComparison.Ordinal));
        }
    }

    private void ShouldNotLeakFromLogs()
    {
        var records = _logger.Collector.GetSnapshot();
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
        records.Where(r => r.Exception != null)
            .ShouldAllBe(r => !r.Exception!.ToString().Contains(Sentinel, StringComparison.Ordinal));
    }

    // ── XacmlXmlPolicySerializer (#1993) ─────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void XacmlXml_MalformedXml_KeepsTheXmlExceptionMessageOutOfErrorActivityAndLogs(bool policySet)
    {
        var xml = $"<Policy xmlns=\"{XacmlNamespace}\"><{Sentinel}></Policy>";
        Should.Throw<XmlException>(() => XDocument.Parse(xml)).Message.ShouldContain(Sentinel);
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = Listen(stopped);
        var sut = new XacmlXmlPolicySerializer(_logger);

        var error = policySet
            ? LeftOf(sut.DeserializePolicySet(xml))
            : LeftOf(sut.DeserializePolicy(xml));

        ShouldNotLeak(error);
        error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.DeserializationFailedCode);
        error.Message.ShouldContain(nameof(XmlException));
        ErrorDescriptions(stopped).ShouldContain(nameof(XmlException));
        ShouldNotLeak(stopped);
        ShouldNotLeakFromLogs();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void XacmlXml_InvalidDocumentContent_ReturnsFixedMessageWithTheExceptionTypeOnly(bool policySet)
    {
        // A root element without its required attributes makes the parser throw InvalidOperationException.
        var element = policySet ? "PolicySet" : "Policy";
        var xml = $"<{element} xmlns=\"{XacmlNamespace}\" />";
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = Listen(stopped);
        var sut = new XacmlXmlPolicySerializer(_logger);

        var error = policySet
            ? LeftOf(sut.DeserializePolicySet(xml))
            : LeftOf(sut.DeserializePolicy(xml));

        error.Message.ShouldBe($"Failed to deserialize {element}: Invalid XACML document ({nameof(InvalidOperationException)}).");
        ErrorDescriptions(stopped).ShouldContain(nameof(InvalidOperationException));
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Message.Contains(nameof(InvalidOperationException)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void XacmlXml_WrongRootElement_RecordsAFixedReasonInTheActivityAndLogs(bool policySet)
    {
        var xml = $"<{Sentinel} xmlns=\"{XacmlNamespace}\" />";
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = Listen(stopped);
        var sut = new XacmlXmlPolicySerializer(_logger);

        var error = policySet
            ? LeftOf(sut.DeserializePolicySet(xml))
            : LeftOf(sut.DeserializePolicy(xml));

        ShouldNotLeak(error);
        error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.DeserializationFailedCode);
        ErrorDescriptions(stopped).ShouldContain("InvalidRootElement");
        ShouldNotLeak(stopped);
        ShouldNotLeakFromLogs();
    }

    // ── DefaultPolicySerializer (#1700) ──────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DefaultSerializer_MalformedJson_KeepsTheJsonExceptionMessageOutOfTheError(bool policySet)
    {
        var json = $"{{\"{Sentinel}\": [1, }}";
        var raw = policySet
            ? Should.Throw<JsonException>(() => JsonSerializer.Deserialize<PolicySet>(json))
            : Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Policy>(json));
        raw.Message.ShouldContain(Sentinel, customMessage: "the marker must reach the raw JsonException for this test to be falsifiable");
        var sut = new DefaultPolicySerializer();

        var error = policySet
            ? LeftOf(sut.DeserializePolicySet(json))
            : LeftOf(sut.DeserializePolicy(json));

        ShouldNotLeak(error);
        error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.DeserializationFailedCode);
        error.Message.ShouldContain(nameof(JsonException));
    }

    // ── FunctionError and the condition evaluator (#1700) ────────────

    [Fact]
    public void FunctionError_BuildsAFixedMessageAndKeepsTheExceptionTypeInTheDetails()
    {
        var error = ABACErrors.FunctionError("my-func", new InvalidOperationException(Sentinel));

        ShouldNotLeak(error);
        error.Message.ShouldBe("Function 'my-func' evaluation failed.");
        error.GetDetails()["exceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public void ConditionEvaluator_FunctionThrows_ReturnsFunctionErrorWithoutTheMessage()
    {
        var function = Substitute.For<IXACMLFunction>();
        function.ReturnType.Returns(XACMLDataTypes.Boolean);
        function.Evaluate(Arg.Any<IReadOnlyList<object?>>()).Returns(_ => throw new InvalidOperationException(Sentinel));
        var registry = new DefaultFunctionRegistry();
        registry.Register("throwing-func", function);
        var sut = new ConditionEvaluator(registry);

        var error = LeftOf(sut.Evaluate(new Apply { FunctionId = "throwing-func", Arguments = [] }, EmptyContext));

        ShouldNotLeak(error);
        error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.FunctionErrorCode);
    }

    [Fact]
    public void RegexFunction_InvalidPattern_ThrowsWithoutTheInnerExceptionMessageAndSurfacesAsFunctionError()
    {
        var registry = new DefaultFunctionRegistry();
        var invalidPattern = $"{Sentinel}(";
        var arguments = new List<object?> { invalidPattern, "input" };

        var thrown = Should.Throw<InvalidOperationException>(
            () => registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!.Evaluate(arguments));

        thrown.InnerException.ShouldBeAssignableTo<ArgumentException>();
        thrown.Message.ShouldNotContain(thrown.InnerException!.Message);
        thrown.Message.ShouldContain(thrown.InnerException.GetType().Name);

        var apply = new Apply
        {
            FunctionId = XACMLFunctionIds.StringRegexpMatch,
            Arguments =
            [
                new AttributeValue { DataType = XACMLDataTypes.String, Value = invalidPattern },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "input" }
            ]
        };
        var error = LeftOf(new ConditionEvaluator(registry).Evaluate(apply, EmptyContext));
        ShouldNotLeak(error);
    }

    // ── EELCompiler (#1700) ──────────────────────────────────────────

    [Fact]
    public async Task EelCompiler_ExpressionThrowsAtRuntime_ReturnsInvalidConditionWithTheExceptionTypeOnly()
    {
        using var compiler = new EELCompiler();
        // The marker is concatenated at run time so the expression text (kept in the error details) never contains it.
        var expression = "((System.Func<bool>)(() => throw new System.InvalidOperationException(\"SENTINEL-\" + \"secret-marker\")))()";

        var result = await compiler.EvaluateAsync(expression, new EELGlobals(), CancellationToken.None);

        var error = LeftOf(result);
        ShouldNotLeak(error);
        error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.InvalidConditionCode);
        error.Message.ShouldContain(nameof(InvalidOperationException));
    }

    private static PolicyEvaluationContext EmptyContext => new()
    {
        SubjectAttributes = new Dictionary<string, AttributeBag>(),
        ResourceAttributes = new Dictionary<string, AttributeBag>(),
        EnvironmentAttributes = new Dictionary<string, AttributeBag>(),
        ActionAttributes = new Dictionary<string, AttributeBag>(),
        RequestType = typeof(object)
    };
}
