using Encina.Messaging.DeadLetter;
using Shouldly;

namespace Encina.GuardTests.Messaging.DeadLetter;

/// <summary>
/// Guard clause tests for <see cref="DeadLetterInputs"/>: the rules throw <see cref="ArgumentException"/>
/// carrying the caller-supplied parameter name.
/// </summary>
public class DeadLetterInputsGuardTests
{
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void RequireUtc_NonUtcInstant_ThrowsArgumentException(DateTimeKind kind)
    {
        var value = DateTime.SpecifyKind(new DateTime(2026, 1, 2, 3, 4, 5), kind);

        var act = () => DeadLetterInputs.RequireUtc(value, "deadLetteredAtUtc");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("deadLetteredAtUtc");
    }

    [Fact]
    public void RequireUtc_UtcInstant_DoesNotThrow()
    {
        var value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Should.NotThrow(() => DeadLetterInputs.RequireUtc(value, "deadLetteredAtUtc"));
    }

    [Fact]
    public void RequireUtc_NullableNonUtcInstant_ThrowsArgumentException()
    {
        DateTime? value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);

        var act = () => DeadLetterInputs.RequireUtc(value, "expiresAtUtc");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("expiresAtUtc");
    }

    [Fact]
    public void RequireUtc_NullableNull_DoesNotThrow()
    {
        DateTime? value = null;

        Should.NotThrow(() => DeadLetterInputs.RequireUtc(value, "expiresAtUtc"));
    }

    [Theory]
    [InlineData(" value")]
    [InlineData("value ")]
    [InlineData("\tvalue")]
    public void RequireTrimmed_PaddedValue_ThrowsArgumentException(string value)
    {
        var act = () => DeadLetterInputs.RequireTrimmed(value, "sourceMessageId");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("sourceMessageId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("value")]
    public void RequireTrimmed_NullEmptyOrTrimmedValue_DoesNotThrow(string? value)
    {
        Should.NotThrow(() => DeadLetterInputs.RequireTrimmed(value, "sourceMessageId"));
    }

    [Fact]
    public void ValidateFilter_PaddedSourcePattern_ThrowsArgumentExceptionNamingTheField()
    {
        var filter = new DeadLetterFilter { SourcePattern = "Outbox " };

        var act = () => DeadLetterInputs.ValidateFilter(filter);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe(nameof(DeadLetterFilter.SourcePattern));
    }

    [Fact]
    public void ValidateFilter_NonUtcAfterInstant_ThrowsArgumentExceptionNamingTheField()
    {
        var filter = new DeadLetterFilter
        {
            DeadLetteredAfterUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local)
        };

        var act = () => DeadLetterInputs.ValidateFilter(filter);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe(nameof(DeadLetterFilter.DeadLetteredAfterUtc));
    }

    [Fact]
    public void ValidateFilter_EmptyFilter_DoesNotThrow()
    {
        Should.NotThrow(() => DeadLetterInputs.ValidateFilter(new DeadLetterFilter()));
    }
}
