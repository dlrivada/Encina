using System.Reflection;

using Shouldly;

using ConsentConversion = Encina.Compliance.Consent.SubjectIdConversion;
using DsrConversion = Encina.Compliance.DataSubjectRights.SubjectIdConversion;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for both copies of the internal <c>SubjectIdConversion</c> helper
/// (<c>Encina.Compliance.DataSubjectRights</c> and <c>Encina.Compliance.Consent</c>).
/// </summary>
[Trait("Category", "Guard")]
public sealed class SubjectIdConversionGuardTests
{
    private static readonly PropertyInfo Property = typeof(Holder).GetProperty(nameof(Holder.Id))!;

    [Fact]
    public void DataSubjectRights_NullProperty_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            DsrConversion.ToInvariantString("id", null!));
        ex.ParamName.ShouldBe("property");
    }

    [Fact]
    public void Consent_NullProperty_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            ConsentConversion.ToInvariantString("id", null!));
        ex.ParamName.ShouldBe("property");
    }

    [Fact]
    public void DataSubjectRights_NullValue_ReturnsNull()
    {
        DsrConversion.ToInvariantString(null, Property).ShouldBeNull();
    }

    [Fact]
    public void Consent_NullValue_ReturnsNull()
    {
        ConsentConversion.ToInvariantString(null, Property).ShouldBeNull();
    }

    [Fact]
    public void DataSubjectRights_UnsupportedType_ThrowsInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(() =>
            DsrConversion.ToInvariantString(1.5d, Property));
    }

    [Fact]
    public void Consent_UnsupportedType_ThrowsInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(() =>
            ConsentConversion.ToInvariantString(1.5d, Property));
    }

    private sealed class Holder
    {
        public object? Id { get; init; }
    }
}
