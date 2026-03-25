using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.Anonymization.Techniques;

using Shouldly;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Extended unit tests for <see cref="GeneralizationTechnique"/> covering
/// DateTime, DateTimeOffset, DateOnly, string, nullable, and unsupported type paths.
/// </summary>
public sealed class GeneralizationTechniqueExtendedTests
{
    private readonly GeneralizationTechnique _technique = new();

    #region CanApply - Additional Types

    [Theory]
    [InlineData(typeof(byte))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(long))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(float))]
    [InlineData(typeof(decimal))]
    public void CanApply_NumericTypes_ShouldReturnTrue(Type type)
    {
        _technique.CanApply(type).ShouldBeTrue();
    }

    [Fact]
    public void CanApply_DateTimeOffsetType_ShouldReturnTrue()
    {
        _technique.CanApply(typeof(DateTimeOffset)).ShouldBeTrue();
    }

    [Fact]
    public void CanApply_DateOnlyType_ShouldReturnTrue()
    {
        _technique.CanApply(typeof(DateOnly)).ShouldBeTrue();
    }

    [Fact]
    public void CanApply_NullableInt_ShouldReturnTrue()
    {
        _technique.CanApply(typeof(int?)).ShouldBeTrue();
    }

    [Fact]
    public void CanApply_NullableDateTime_ShouldReturnTrue()
    {
        _technique.CanApply(typeof(DateTime?)).ShouldBeTrue();
    }

    [Fact]
    public void CanApply_GuidType_ShouldReturnFalse()
    {
        _technique.CanApply(typeof(Guid)).ShouldBeFalse();
    }

    [Fact]
    public void CanApply_NullType_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _technique.CanApply(null!));
    }

    #endregion

    #region ApplyAsync - DateTime Generalization

    [Fact]
    public async Task ApplyAsync_DateTime_GranularityYear_ShouldTruncateToYear()
    {
        var dt = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var parameters = new Dictionary<string, object> { ["Granularity"] = 1 };

        var result = await _technique.ApplyAsync(dt, typeof(DateTime), parameters);

        result.Match(
            Right: v =>
            {
                var r = (DateTime)v!;
                r.Year.ShouldBe(2025);
                r.Month.ShouldBe(1);
                r.Day.ShouldBe(1);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_DateTime_GranularityMonth_ShouldTruncateToMonth()
    {
        var dt = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var parameters = new Dictionary<string, object> { ["Granularity"] = 2 };

        var result = await _technique.ApplyAsync(dt, typeof(DateTime), parameters);

        result.Match(
            Right: v =>
            {
                var r = (DateTime)v!;
                r.Year.ShouldBe(2025);
                r.Month.ShouldBe(6);
                r.Day.ShouldBe(1);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_DateTime_GranularityDay_ShouldTruncateToDay()
    {
        var dt = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var parameters = new Dictionary<string, object> { ["Granularity"] = 3 };

        var result = await _technique.ApplyAsync(dt, typeof(DateTime), parameters);

        result.Match(
            Right: v =>
            {
                var r = (DateTime)v!;
                r.Year.ShouldBe(2025);
                r.Month.ShouldBe(6);
                r.Day.ShouldBe(15);
                r.Hour.ShouldBe(0);
                r.Minute.ShouldBe(0);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region ApplyAsync - DateTimeOffset Generalization

    [Fact]
    public async Task ApplyAsync_DateTimeOffset_GranularityYear_ShouldTruncateToYear()
    {
        var dto = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
        var parameters = new Dictionary<string, object> { ["Granularity"] = 1 };

        var result = await _technique.ApplyAsync(dto, typeof(DateTimeOffset), parameters);

        result.Match(
            Right: v =>
            {
                var r = (DateTimeOffset)v!;
                r.Year.ShouldBe(2025);
                r.Month.ShouldBe(1);
                r.Day.ShouldBe(1);
                r.Offset.ShouldBe(TimeSpan.FromHours(2));
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_DateTimeOffset_GranularityMonth_ShouldTruncateToMonth()
    {
        var dto = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);
        var parameters = new Dictionary<string, object> { ["Granularity"] = 2 };

        var result = await _technique.ApplyAsync(dto, typeof(DateTimeOffset), parameters);

        result.Match(
            Right: v =>
            {
                var r = (DateTimeOffset)v!;
                r.Month.ShouldBe(6);
                r.Day.ShouldBe(1);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_DateTimeOffset_GranularityDay_ShouldTruncateToDay()
    {
        var dto = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);
        var parameters = new Dictionary<string, object> { ["Granularity"] = 3 };

        var result = await _technique.ApplyAsync(dto, typeof(DateTimeOffset), parameters);

        result.Match(
            Right: v =>
            {
                var r = (DateTimeOffset)v!;
                r.Day.ShouldBe(15);
                r.Hour.ShouldBe(0);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region ApplyAsync - DateOnly Generalization

    [Fact]
    public async Task ApplyAsync_DateOnly_GranularityYear_ShouldTruncateToYear()
    {
        var d = new DateOnly(2025, 6, 15);
        var parameters = new Dictionary<string, object> { ["Granularity"] = 1 };

        var result = await _technique.ApplyAsync(d, typeof(DateOnly), parameters);

        result.Match(
            Right: v =>
            {
                var r = (DateOnly)v!;
                r.Year.ShouldBe(2025);
                r.Month.ShouldBe(1);
                r.Day.ShouldBe(1);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_DateOnly_GranularityMonth_ShouldTruncateToMonth()
    {
        var d = new DateOnly(2025, 6, 15);
        var parameters = new Dictionary<string, object> { ["Granularity"] = 2 };

        var result = await _technique.ApplyAsync(d, typeof(DateOnly), parameters);

        result.Match(
            Right: v =>
            {
                var r = (DateOnly)v!;
                r.Month.ShouldBe(6);
                r.Day.ShouldBe(1);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_DateOnly_GranularityDay_ShouldReturnSameDate()
    {
        var d = new DateOnly(2025, 6, 15);
        var parameters = new Dictionary<string, object> { ["Granularity"] = 3 };

        var result = await _technique.ApplyAsync(d, typeof(DateOnly), parameters);

        result.Match(
            Right: v => ((DateOnly)v!).ShouldBe(d),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region ApplyAsync - String Generalization

    [Fact]
    public async Task ApplyAsync_String_DefaultPreserve_ShouldMaskAfterThreeChars()
    {
        var result = await _technique.ApplyAsync("HelloWorld", typeof(string), null);

        result.Match(
            Right: v => ((string)v!).ShouldBe("Hel*******"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_String_CustomPreserveLength_ShouldMaskAfterGranularity()
    {
        var parameters = new Dictionary<string, object> { ["Granularity"] = 5 };

        var result = await _technique.ApplyAsync("HelloWorld", typeof(string), parameters);

        result.Match(
            Right: v => ((string)v!).ShouldBe("Hello*****"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_String_ShorterThanPreserve_ShouldReturnOriginal()
    {
        var result = await _technique.ApplyAsync("Hi", typeof(string), null);

        result.Match(
            Right: v => ((string)v!).ShouldBe("Hi"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_String_ExactPreserveLength_ShouldReturnOriginal()
    {
        var result = await _technique.ApplyAsync("Hey", typeof(string), null);

        result.Match(
            Right: v => ((string)v!).ShouldBe("Hey"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region ApplyAsync - Unsupported Type

    [Fact]
    public async Task ApplyAsync_UnsupportedType_ShouldReturnLeft()
    {
        var result = await _technique.ApplyAsync(Guid.NewGuid(), typeof(Guid), null);

        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldNotBeNullOrWhiteSpace());
    }

    #endregion

    #region ApplyAsync - Null Value

    [Fact]
    public async Task ApplyAsync_NullValue_ShouldThrowValueIsNullException()
    {
        // LanguageExt's Right(null) throws ValueIsNullException
        await Should.ThrowAsync<LanguageExt.ValueIsNullException>(
            async () => await _technique.ApplyAsync(null, typeof(string), null));
    }

    #endregion

    #region ApplyAsync - Null ValueType

    [Fact]
    public async Task ApplyAsync_NullValueType_ShouldThrowArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _technique.ApplyAsync("test", null!, null));
    }

    #endregion

    #region ApplyAsync - Decimal Numeric

    [Fact]
    public async Task ApplyAsync_Decimal_ShouldGeneralizeToRange()
    {
        var parameters = new Dictionary<string, object> { ["Granularity"] = 100 };

        var result = await _technique.ApplyAsync(250m, typeof(decimal), parameters);

        result.Match(
            Right: v => ((string)v!).ShouldBe("200-299"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion
}
