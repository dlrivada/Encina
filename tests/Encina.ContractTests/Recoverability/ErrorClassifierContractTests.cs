using Encina.Recoverability;
using Shouldly;

namespace Encina.ContractTests.Recoverability;

/// <summary>
/// Contract of <see cref="IErrorClassifier"/> as <see cref="DefaultErrorClassifier"/> honours it: a total,
/// deterministic function with only the three defined outcomes.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ErrorClassifierContractTests
{
    private static readonly DefaultErrorClassifier Classifier = new();

    public static TheoryData<EncinaError, Exception?> Samples() => new()
    {
        { EncinaError.New("plain"), null },
        { EncinaError.New(new TimeoutException("t")), null },
        { EncinaErrors.Create("encina.outbound.client_error", "m", details: new Dictionary<string, object?> { ["transient"] = false }), null },
        { EncinaErrors.Create("encina.outbound.server_error", "m", details: new Dictionary<string, object?> { ["transient"] = true }), null },
        { EncinaError.New("validation"), new ArgumentException("a") },
        { default, null },
        { EncinaError.New("pool"), new InvalidOperationException("max pool size was reached") },
    };

    [Fact]
    public void DefaultErrorClassifier_ImplementsTheInterface_AndIsSealed()
    {
        typeof(IErrorClassifier).IsAssignableFrom(typeof(DefaultErrorClassifier)).ShouldBeTrue();
        typeof(DefaultErrorClassifier).IsSealed.ShouldBeTrue();
        typeof(IErrorClassifier).Namespace.ShouldBe("Encina.Recoverability");
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void Classify_NeverThrows_IsDeterministic_AndReturnsADefinedValue(EncinaError error, Exception? exception)
    {
        var first = Classifier.Classify(error, exception);
        var second = Classifier.Classify(error, exception);

        first.ShouldBe(second);
        Enum.IsDefined(first).ShouldBeTrue();
    }

    [Fact]
    public void ErrorClassification_KeepsItsNumericContract()
    {
        ((int)ErrorClassification.Unknown).ShouldBe(0);
        ((int)ErrorClassification.Transient).ShouldBe(1);
        ((int)ErrorClassification.Permanent).ShouldBe(2);
    }

    [Fact]
    public void Classify_DeclaredTransientMetadata_IsHonoured()
    {
        var transient = EncinaErrors.Create("any.code", "m", details: new Dictionary<string, object?> { ["transient"] = true });
        var permanent = EncinaErrors.Create("any.code", "m", details: new Dictionary<string, object?> { ["transient"] = false });

        Classifier.Classify(transient, null).ShouldBe(ErrorClassification.Transient);
        Classifier.Classify(permanent, null).ShouldBe(ErrorClassification.Permanent);
    }
}
