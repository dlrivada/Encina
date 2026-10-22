using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for the metadata members of <see cref="ReadAuditContext"/> (break-the-glass links its
/// elevation id to the read audit trail through them).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Audit")]
public sealed class ReadAuditContextMetadataTests
{
    [Fact]
    public void Metadata_Initially_IsEmpty()
    {
        new ReadAuditContext().Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void WithMetadata_StoresEntryAndReturnsSameInstance()
    {
        var sut = new ReadAuditContext();

        var returned = sut.WithMetadata("elevation_id", "abc");

        returned.ShouldBeSameAs(sut);
        sut.Metadata["elevation_id"].ShouldBe("abc");
    }

    [Fact]
    public void WithMetadata_SameKeyTwice_ReplacesValue()
    {
        var sut = new ReadAuditContext();
        sut.WithMetadata("k", "1").WithMetadata("k", "2");

        sut.Metadata.Count.ShouldBe(1);
        sut.Metadata["k"].ShouldBe("2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void WithMetadata_BlankKey_Throws(string? key)
    {
        Should.Throw<ArgumentException>(() => new ReadAuditContext().WithMetadata(key!, "v"));
    }

    [Fact]
    public void WithMetadata_NullValue_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new ReadAuditContext().WithMetadata("k", null!));
    }
}
