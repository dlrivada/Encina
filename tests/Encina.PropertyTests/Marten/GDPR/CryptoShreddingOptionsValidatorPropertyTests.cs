using Encina.Marten.GDPR;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Marten.GDPR;

/// <summary>
/// Property-based tests for <see cref="CryptoShreddingOptionsValidator"/> invariants.
/// Verifies the validator's decisions over randomly generated option instances.
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "Marten")]
public sealed class CryptoShreddingOptionsValidatorPropertyTests
{
    private static readonly CryptoShreddingOptionsValidator Sut = new();

    // ─── KeyRotationDays invariants ───

    [Property(MaxTest = 200)]
    public bool Validate_PositiveRotationAndNonEmptyPlaceholder_Succeeds(PositiveInt days, NonEmptyString placeholder)
    {
        var options = new CryptoShreddingOptions
        {
            KeyRotationDays = days.Get,
            AnonymizedPlaceholder = placeholder.Get
        };

        // If the placeholder is whitespace-only, fail; otherwise expect Succeeded.
        var result = Sut.Validate(null, options);

        if (string.IsNullOrWhiteSpace(placeholder.Get))
        {
            return result.Failed;
        }
        return result.Succeeded;
    }

    [Property(MaxTest = 200)]
    public bool Validate_ZeroOrNegativeRotation_AlwaysFails(NonEmptyString placeholder)
    {
        foreach (var days in new[] { 0, -1, -100, int.MinValue })
        {
            var options = new CryptoShreddingOptions
            {
                KeyRotationDays = days,
                AnonymizedPlaceholder = string.IsNullOrWhiteSpace(placeholder.Get) ? "[REDACTED]" : placeholder.Get
            };

            var result = Sut.Validate(null, options);
            if (!result.Failed) return false;
        }
        return true;
    }

    [Property(MaxTest = 200)]
    public bool Validate_NegativeRotation_FailureMessageMentionsKeyRotationDays(NegativeInt days)
    {
        var options = new CryptoShreddingOptions
        {
            KeyRotationDays = days.Get,
            AnonymizedPlaceholder = "[REDACTED]"
        };

        var result = Sut.Validate(null, options);
        if (!result.Failed) return false;

        return result.Failures!.Any(f => f.Contains("KeyRotationDays", StringComparison.Ordinal));
    }

    // ─── Placeholder invariants ───

    [Property(MaxTest = 200)]
    public bool Validate_EmptyOrWhitespacePlaceholder_AlwaysFails(PositiveInt days)
    {
        foreach (var placeholder in new[] { "", " ", "\t", "\n", "   " })
        {
            var options = new CryptoShreddingOptions
            {
                KeyRotationDays = days.Get,
                AnonymizedPlaceholder = placeholder
            };

            var result = Sut.Validate(null, options);
            if (!result.Failed) return false;
        }
        return true;
    }

    [Property(MaxTest = 200)]
    public bool Validate_WhitespacePlaceholder_FailureMessageMentionsAnonymizedPlaceholder(PositiveInt days)
    {
        var options = new CryptoShreddingOptions
        {
            KeyRotationDays = days.Get,
            AnonymizedPlaceholder = "   "
        };

        var result = Sut.Validate(null, options);
        if (!result.Failed) return false;

        return result.Failures!.Any(f => f.Contains("AnonymizedPlaceholder", StringComparison.Ordinal));
    }

    // ─── Combined invariants ───

    [Property(MaxTest = 200)]
    public bool Validate_BothInvalid_ReportsBothFailures(NegativeInt days)
    {
        var options = new CryptoShreddingOptions
        {
            KeyRotationDays = days.Get,
            AnonymizedPlaceholder = ""
        };

        var result = Sut.Validate(null, options);
        if (!result.Failed) return false;

        var failures = result.Failures!.ToList();
        return failures.Count >= 2
               && failures.Any(f => f.Contains("KeyRotationDays", StringComparison.Ordinal))
               && failures.Any(f => f.Contains("AnonymizedPlaceholder", StringComparison.Ordinal));
    }

    [Property(MaxTest = 200)]
    public bool Validate_WithName_ProducesSameResultAsNull(PositiveInt days, NonEmptyString placeholder, NonEmptyString name)
    {
        // The name parameter must not affect the validation outcome.
        if (string.IsNullOrWhiteSpace(placeholder.Get)) return true;

        var options = new CryptoShreddingOptions
        {
            KeyRotationDays = days.Get,
            AnonymizedPlaceholder = placeholder.Get
        };

        var withNull = Sut.Validate(null, options);
        var withName = Sut.Validate(name.Get, options);

        return withNull.Succeeded == withName.Succeeded;
    }

    // ─── NullOptions guard ───

    [Property(MaxTest = 50)]
    public bool Validate_NullOptions_ThrowsArgumentNullException(NonEmptyString name)
    {
        try
        {
            Sut.Validate(name.Get, null!);
            return false;
        }
        catch (ArgumentNullException)
        {
            return true;
        }
    }

    // ─── Idempotence ───

    [Property(MaxTest = 100)]
    public bool Validate_SameInput_YieldsSameResult(PositiveInt days, NonEmptyString placeholder)
    {
        if (string.IsNullOrWhiteSpace(placeholder.Get)) return true;

        var options = new CryptoShreddingOptions
        {
            KeyRotationDays = days.Get,
            AnonymizedPlaceholder = placeholder.Get
        };

        var first = Sut.Validate(null, options);
        var second = Sut.Validate(null, options);

        return first.Succeeded == second.Succeeded && first.Failed == second.Failed;
    }

    // ─── Defaults ───

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = Sut.Validate(null, new CryptoShreddingOptions());
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_CustomPositiveDaysAndValidPlaceholder_Succeeds()
    {
        var result = Sut.Validate(null, new CryptoShreddingOptions
        {
            KeyRotationDays = 180,
            AnonymizedPlaceholder = "<HIDDEN>"
        });
        Assert.True(result.Succeeded);
    }
}
