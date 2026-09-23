using System.Globalization;
using System.Reflection;

using ConsentConversion = Encina.Compliance.Consent.SubjectIdConversion;
using DsrConversion = Encina.Compliance.DataSubjectRights.SubjectIdConversion;

namespace Encina.UnitTests.Compliance;

/// <summary>
/// Tests for the two copies of the internal <c>SubjectIdConversion</c> helper (#1149, #1159 review):
/// <c>Encina.Compliance.DataSubjectRights.SubjectIdConversion</c> and
/// <c>Encina.Compliance.Consent.SubjectIdConversion</c>. The packages share no internal assembly, so the
/// helper is duplicated; <see cref="BothCopies_BehaveIdentically"/> keeps the copies in lockstep.
/// </summary>
public sealed class SubjectIdConversionTests
{
    private static readonly PropertyInfo Property = typeof(Holder).GetProperty(nameof(Holder.Id))!;

    private static readonly Guid SampleGuid = Guid.Parse("7f3a2c1e-4b5d-4e6f-8a9b-0c1d2e3f4a5b");

    // ─── Supported shapes ───

    public static TheoryData<object?, string?> SupportedValues() => new()
    {
        { null, null },
        { "patient-1", "patient-1" },
        { "", null },
        { "   ", null },
        { SampleGuid, "7f3a2c1e-4b5d-4e6f-8a9b-0c1d2e3f4a5b" },
        { Guid.Empty, null },
        { 0, "0" },
        { 42, "42" },
        { -7L, "-7" },
        { (byte)5, "5" },
        { ulong.MaxValue, "18446744073709551615" },
        { (Int128)123, "123" },
        { new RecordStructId(SampleGuid), "7f3a2c1e-4b5d-4e6f-8a9b-0c1d2e3f4a5b" },
        { new RecordStructId(Guid.Empty), null },
        { new RecordClassId(99), "99" },
        { new PlainStructId("abc"), "abc" },
        { new PlainStructId(""), null },
        { new NullableValueId(null), null },
        { new NullableValueId(12), "12" },
        { new FormattableId(8), "id-8" },
    };

    [Theory]
    [MemberData(nameof(SupportedValues))]
    public void DataSubjectRightsCopy_ConvertsSupportedShapes(object? value, string? expected)
    {
        DsrConversion.ToInvariantString(value, Property).ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(SupportedValues))]
    public void ConsentCopy_ConvertsSupportedShapes(object? value, string? expected)
    {
        ConsentConversion.ToInvariantString(value, Property).ShouldBe(expected);
    }

    [Fact]
    public void Integers_UseInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            // A culture with a non-ASCII negative sign would change int.ToString()
            CultureInfo.CurrentCulture = new CultureInfo("sv-SE");

            DsrConversion.ToInvariantString(-1234, Property).ShouldBe("-1234");
            ConsentConversion.ToInvariantString(-1234, Property).ShouldBe("-1234");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void RecordStruct_NeverUsesTheCompilerGeneratedToString()
    {
        var id = new RecordStructId(SampleGuid);

        var result = DsrConversion.ToInvariantString(id, Property)!;

        result.ShouldNotContain("RecordStructId");
        result.ShouldNotContain("Value =");
    }

    // ─── Unsupported shapes are configuration errors ───

    public static TheoryData<object> UnsupportedValues() => new()
    {
        new object(),
        1.5d,
        2.5m,
        DateTime.UnixEpoch,
        TimeSpan.FromSeconds(1),
        DayOfWeek.Monday,
        SampleKind.First,
        new NoValueStructId(3),
        new UnsupportedValueId(1.5d),
        new ToStringOnlyId("x"),
    };

    [Theory]
    [MemberData(nameof(UnsupportedValues))]
    public void DataSubjectRightsCopy_ThrowsForUnsupportedShapes(object value)
    {
        var exception = Should.Throw<InvalidOperationException>(() => DsrConversion.ToInvariantString(value, Property));

        exception.Message.ShouldContain($"{nameof(Holder)}.{nameof(Holder.Id)}");
        exception.Message.ShouldContain(value.GetType().Name);
    }

    [Theory]
    [MemberData(nameof(UnsupportedValues))]
    public void ConsentCopy_ThrowsForUnsupportedShapes(object value)
    {
        Should.Throw<InvalidOperationException>(() => ConsentConversion.ToInvariantString(value, Property));
    }

    // ─── Parity between the two copies ───

    [Fact]
    public void BothCopies_BehaveIdentically()
    {
        var inputs = SupportedValues().Select(row => row.Data.Item1)
            .Concat(UnsupportedValues().Select(row => (object?)row.Data))
            .ToList();

        foreach (var input in inputs)
        {
            Describe(() => DsrConversion.ToInvariantString(input, Property))
                .ShouldBe(Describe(() => ConsentConversion.ToInvariantString(input, Property)),
                    $"input of type {input?.GetType().Name ?? "null"}");
        }
    }

    private static string Describe(Func<string?> conversion)
    {
        try
        {
            return "value:" + (conversion() ?? "<null>");
        }
        catch (Exception ex)
        {
            return "throws:" + ex.GetType().Name + ":" + ex.Message;
        }
    }

    // ─── Test shapes ───

    private sealed class Holder
    {
        public object? Id { get; init; }
    }

    public readonly record struct RecordStructId(Guid Value);

    public sealed record RecordClassId(int Value);

    public readonly struct PlainStructId
    {
        public PlainStructId(string value) => Value = value;

        public string Value { get; }
    }

    public readonly record struct NullableValueId(long? Value);

    public readonly struct FormattableId : IFormattable
    {
        private readonly int _number;

        public FormattableId(int number) => _number = number;

        public string ToString(string? format, IFormatProvider? formatProvider) =>
            "id-" + _number.ToString(formatProvider);

        public override string ToString() => ToString(null, CultureInfo.InvariantCulture);
    }

    public readonly struct NoValueStructId
    {
        public NoValueStructId(int number) => Number = number;

        public int Number { get; }
    }

    public readonly record struct UnsupportedValueId(double Value);

    public sealed class ToStringOnlyId
    {
        private readonly string _raw;

        public ToStringOnlyId(string raw) => _raw = raw;

        public override string ToString() => _raw;
    }

    public enum SampleKind
    {
        First
    }
}
