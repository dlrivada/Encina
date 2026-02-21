using Encina.Security.PII;
using Encina.Security.PII.Abstractions;

namespace Encina.UnitTests.Security.PII;

public sealed class PIIOptionsTests
{
    #region Default Values

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new PIIOptions();

        options.DefaultMode.ShouldBe(MaskingMode.Partial);
        options.MaskInResponses.ShouldBeTrue();
        options.MaskInLogs.ShouldBeTrue();
        options.MaskInAuditTrails.ShouldBeTrue();
        options.EnableTracing.ShouldBeFalse();
        options.EnableMetrics.ShouldBeFalse();
        options.AddHealthCheck.ShouldBeFalse();
    }

    #endregion

    #region AddStrategy

    [Fact]
    public void AddStrategy_RegistersCustomStrategy()
    {
        var options = new PIIOptions();

        options.AddStrategy<FakeEmailStrategy>(PIIType.Email);

        options.CustomStrategies.ShouldContainKey(PIIType.Email);
        options.CustomStrategies[PIIType.Email].ShouldBe(typeof(FakeEmailStrategy));
    }

    [Fact]
    public void AddStrategy_OverridesSameType()
    {
        var options = new PIIOptions();

        options.AddStrategy<FakeEmailStrategy>(PIIType.Email);
        options.AddStrategy<AnotherFakeStrategy>(PIIType.Email);

        options.CustomStrategies[PIIType.Email].ShouldBe(typeof(AnotherFakeStrategy));
    }

    [Fact]
    public void AddStrategy_ReturnsOptionsForChaining()
    {
        var options = new PIIOptions();

        var result = options.AddStrategy<FakeEmailStrategy>(PIIType.Email);

        result.ShouldBeSameAs(options);
    }

    #endregion

    #region SensitiveFieldPatterns

    [Fact]
    public void SensitiveFieldPatterns_DefaultContainsExpectedPatterns()
    {
        var options = new PIIOptions();

        options.SensitiveFieldPatterns.ShouldContain("password");
        options.SensitiveFieldPatterns.ShouldContain("token");
        options.SensitiveFieldPatterns.ShouldContain("secret");
        options.SensitiveFieldPatterns.ShouldContain("apikey");
        options.SensitiveFieldPatterns.ShouldContain("api_key");
        options.SensitiveFieldPatterns.ShouldContain("creditcard");
        options.SensitiveFieldPatterns.ShouldContain("credit_card");
        options.SensitiveFieldPatterns.ShouldContain("ssn");
        options.SensitiveFieldPatterns.ShouldContain("socialSecurity");
    }

    [Fact]
    public void AddSensitiveFieldPattern_AddsPattern()
    {
        var options = new PIIOptions();

        options.AddSensitiveFieldPattern("taxId");

        options.SensitiveFieldPatterns.ShouldContain("taxId");
    }

    [Fact]
    public void AddSensitiveFieldPattern_ReturnsOptionsForChaining()
    {
        var options = new PIIOptions();

        var result = options.AddSensitiveFieldPattern("passport");

        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddSensitiveFieldPattern_NullPattern_ThrowsArgumentNull()
    {
        var options = new PIIOptions();

        Should.Throw<ArgumentNullException>(() => options.AddSensitiveFieldPattern(null!));
    }

    [Fact]
    public void AddSensitiveFieldPattern_WhitespacePattern_ThrowsArgumentException()
    {
        var options = new PIIOptions();

        Should.Throw<ArgumentException>(() => options.AddSensitiveFieldPattern("   "));
    }

    [Fact]
    public void RemoveSensitiveFieldPattern_RemovesPattern()
    {
        var options = new PIIOptions();

        options.RemoveSensitiveFieldPattern("password");

        options.SensitiveFieldPatterns.ShouldNotContain("password");
    }

    [Fact]
    public void RemoveSensitiveFieldPattern_ReturnsOptionsForChaining()
    {
        var options = new PIIOptions();

        var result = options.RemoveSensitiveFieldPattern("password");

        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void RemoveSensitiveFieldPattern_NullPattern_ThrowsArgumentNull()
    {
        var options = new PIIOptions();

        Should.Throw<ArgumentNullException>(() => options.RemoveSensitiveFieldPattern(null!));
    }

    [Fact]
    public void RemoveSensitiveFieldPattern_NonExistentPattern_DoesNotThrow()
    {
        var options = new PIIOptions();

        Should.NotThrow(() => options.RemoveSensitiveFieldPattern("nonexistent"));
    }

    #endregion

    #region Properties Settable

    [Fact]
    public void Properties_AreSettable()
    {
        var options = new PIIOptions
        {
            DefaultMode = MaskingMode.Full,
            MaskInResponses = false,
            MaskInLogs = false,
            MaskInAuditTrails = false,
            EnableTracing = true,
            EnableMetrics = true,
            AddHealthCheck = true
        };

        options.DefaultMode.ShouldBe(MaskingMode.Full);
        options.MaskInResponses.ShouldBeFalse();
        options.MaskInLogs.ShouldBeFalse();
        options.MaskInAuditTrails.ShouldBeFalse();
        options.EnableTracing.ShouldBeTrue();
        options.EnableMetrics.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeTrue();
    }

    #endregion

    #region Test Strategies

    private sealed class FakeEmailStrategy : IMaskingStrategy
    {
        public string Apply(string value, MaskingOptions options) => "***@fake.com";
    }

    private sealed class AnotherFakeStrategy : IMaskingStrategy
    {
        public string Apply(string value, MaskingOptions options) => "[MASKED]";
    }

    #endregion
}
