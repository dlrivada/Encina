using System.Globalization;
using System.Reflection;
using Encina.Security.BreakTheGlass;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

public sealed record IntWrapperId(int Value);

public sealed record NullableIntWrapperId(int? Value);

public sealed class FormattableId(string text) : IFormattable
{
    public string ToString(string? format, IFormatProvider? formatProvider) => text;
}

public sealed class PlainClassId
{
    public string Name { get; init; } = "x";
}

public sealed record DoubleValueId(double Value);

public class BaseValueId
{
    public string Value { get; init; } = "base";
}

public sealed class HiddenValueId : BaseValueId
{
    public new int Value { get; init; } = 1;
}

public interface IFormattableKey : IFormattable;

public enum PatientKind
{
    First
}

public sealed class SubjectCarrier
{
    public object? Subject { get; set; }
}

/// <summary>Unit tests for <see cref="BreakTheGlassSubjectIdConversion"/> (internal; reached through InternalsVisibleTo).</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassSubjectIdConversionTests
{
    private static readonly PropertyInfo Property = typeof(SubjectCarrier).GetProperty(nameof(SubjectCarrier.Subject))!;

    private static string? Convert(object? value) => BreakTheGlassSubjectIdConversion.ToInvariantString(value, Property);

    [Fact]
    public void Null_IsMissing() => Convert(null).ShouldBeNull();

    [Theory]
    [InlineData("p-1", "p-1")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void String_IsUsedAsIs_BlankIsMissing(string value, string? expected) => Convert(value).ShouldBe(expected);

    [Fact]
    public void Guid_UsesDFormat_EmptyIsMissing()
    {
        var id = Guid.NewGuid();

        Convert(id).ShouldBe(id.ToString("D"));
        Convert(Guid.Empty).ShouldBeNull();
    }

    [Fact]
    public void Integers_UseTheInvariantCulture_ZeroIsAValidId()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            Convert(1234567).ShouldBe("1234567");
            Convert(-5L).ShouldBe("-5");
            Convert(0).ShouldBe("0");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void RecordWithStringValue_IsUnwrapped_NotTheGeneratedToString()
    {
        Convert(new PatientId("p-1")).ShouldBe("p-1");
        Convert(new PatientId("")).ShouldBeNull();
    }

    [Fact]
    public void ReadonlyRecordStructWithGuidValue_IsUnwrapped()
    {
        var id = Guid.NewGuid();

        Convert(new GuidPatientId(id)).ShouldBe(id.ToString("D"));
        Convert(default(GuidPatientId)).ShouldBeNull();
    }

    [Fact]
    public void WrapperWithIntegerValue_IsUnwrapped()
    {
        Convert(new IntWrapperId(42)).ShouldBe("42");
        Convert(new NullableIntWrapperId(7)).ShouldBe("7");
        Convert(new NullableIntWrapperId(null)).ShouldBeNull();
    }

    [Fact]
    public void FormattableId_UsesInvariantFormatting_BlankAndZeroGuidAreMissing()
    {
        Convert(new FormattableId("k-9")).ShouldBe("k-9");
        Convert(new FormattableId(" ")).ShouldBeNull();
        Convert(new FormattableId(Guid.Empty.ToString())).ShouldBeNull();
    }

    [Fact]
    public void UnsupportedShape_Throws()
    {
        object[] unsupported =
        [
            PatientKind.First,
            1.5d,
            DateTime.UnixEpoch,
            new PlainClassId(),
            new DoubleValueId(1.5d),
            new HiddenValueId(),
            new object()
        ];

        foreach (var value in unsupported)
        {
            Should.Throw<InvalidOperationException>(() => Convert(value));
        }
    }

    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(Guid?), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(PatientId), true)]
    [InlineData(typeof(GuidPatientId), true)]
    [InlineData(typeof(GuidPatientId?), true)]
    [InlineData(typeof(FormattableId), true)]
    [InlineData(typeof(IFormattableKey), true)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(double), false)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(PatientKind), false)]
    [InlineData(typeof(PlainClassId), false)]
    [InlineData(typeof(DoubleValueId), false)]
    [InlineData(typeof(HiddenValueId), false)]
    public void IsSupportedType_FollowsTheExtractorRules(Type type, bool expected) =>
        BreakTheGlassSubjectIdConversion.IsSupportedType(type).ShouldBe(expected);
}
