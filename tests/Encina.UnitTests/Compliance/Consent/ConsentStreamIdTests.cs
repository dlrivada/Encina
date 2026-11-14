using Encina.Compliance.Consent;
using Shouldly;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="ConsentStreamId"/>, the deterministic consent stream id (#3769).
/// </summary>
public class ConsentStreamIdTests
{
    [Fact]
    public void For_SameKey_ReturnsSameId()
    {
        var first = ConsentStreamId.For("tenant-a", "subject-1", "marketing");
        var second = ConsentStreamId.For("tenant-a", "subject-1", "marketing");

        second.ShouldBe(first);
    }

    [Fact]
    public void For_KnownKeys_MatchPinnedValues()
    {
        // Stream ids are persisted: these vectors fail if the namespace or the encoding ever change.
        ConsentStreamId.For(null, "subject-1", "marketing")
            .ShouldBe(new Guid("315338fc-b6ab-844d-ab01-3c4049296b17"));
        ConsentStreamId.For("tenant-a", "subject-1", "marketing")
            .ShouldBe(new Guid("c8c8311f-4d5f-89ad-9ad9-5fb58df50f55"));
    }

    [Fact]
    public void For_NullAndEmptyTenant_ReturnSameId()
    {
        var withNull = ConsentStreamId.For(null, "subject-1", "marketing");
        var withEmpty = ConsentStreamId.For(string.Empty, "subject-1", "marketing");

        withEmpty.ShouldBe(withNull);
    }

    [Fact]
    public void For_NoTenant_IsTheNoTenantKeyPart()
    {
        ConsentStreamId.For(ConsentStreamId.NoTenant, "subject-1", "marketing")
            .ShouldBe(ConsentStreamId.For(null, "subject-1", "marketing"));
    }

    [Fact]
    public void For_DifferentTenant_ReturnsDifferentId()
    {
        ConsentStreamId.For("tenant-a", "subject-1", "marketing")
            .ShouldNotBe(ConsentStreamId.For("tenant-b", "subject-1", "marketing"));
        ConsentStreamId.For("tenant-a", "subject-1", "marketing")
            .ShouldNotBe(ConsentStreamId.For(null, "subject-1", "marketing"));
    }

    [Fact]
    public void For_DifferentSubject_ReturnsDifferentId()
    {
        ConsentStreamId.For("tenant-a", "subject-1", "marketing")
            .ShouldNotBe(ConsentStreamId.For("tenant-a", "subject-2", "marketing"));
    }

    [Fact]
    public void For_DifferentPurpose_ReturnsDifferentId()
    {
        ConsentStreamId.For("tenant-a", "subject-1", "marketing")
            .ShouldNotBe(ConsentStreamId.For("tenant-a", "subject-1", "analytics"));
    }

    [Fact]
    public void For_SubjectAndPurposeAreOrdinal_CaseAndWhitespaceMatter()
    {
        var baseline = ConsentStreamId.For("tenant-a", "subject-1", "marketing");

        ConsentStreamId.For("tenant-a", "Subject-1", "marketing").ShouldNotBe(baseline);
        ConsentStreamId.For("tenant-a", "subject-1", "Marketing").ShouldNotBe(baseline);
        ConsentStreamId.For("tenant-a", "subject-1 ", "marketing").ShouldNotBe(baseline);
    }

    [Fact]
    public void For_PartsCannotCollideAcrossBoundaries()
    {
        // Without length prefixes "ab" + "c" and "a" + "bc" would hash the same bytes.
        ConsentStreamId.For("t", "ab", "c").ShouldNotBe(ConsentStreamId.For("t", "a", "bc"));
        ConsentStreamId.For("t|s", "p", "x").ShouldNotBe(ConsentStreamId.For("t", "s|p", "x"));
    }

    [Fact]
    public void Derive_LengthPrefixesSeparateParts()
    {
        var ns = ConsentStreamId.ConsentNamespace;

        ConsentStreamId.Derive(ns, "ab", "c").ShouldNotBe(ConsentStreamId.Derive(ns, "a", "bc"));
        ConsentStreamId.Derive(ns, "abc").ShouldNotBe(ConsentStreamId.Derive(ns, "ab", "c"));
        ConsentStreamId.Derive(ns, "a", string.Empty).ShouldNotBe(ConsentStreamId.Derive(ns, "a"));
    }

    [Fact]
    public void Derive_DifferentNamespace_ReturnsDifferentId()
    {
        var otherNamespace = new Guid("11111111-2222-3333-4444-555555555555");

        ConsentStreamId.Derive(otherNamespace, "-", "subject-1", "marketing")
            .ShouldNotBe(ConsentStreamId.For(null, "subject-1", "marketing"));
    }

    [Fact]
    public void Derive_SetsVersion8AndRfcVariant()
    {
        var id = ConsentStreamId.For("tenant-a", "subject-1", "marketing");

        id.Version.ShouldBe(8);
        // RFC 9562 variant is 0b10 in the two high bits of octet 8: the high nibble is 8 to 11.
        id.Variant.ShouldBeInRange(8, 11);
    }

    [Fact]
    public void Derive_NonAsciiParts_AreEncodedAsUtf8()
    {
        var ns = ConsentStreamId.ConsentNamespace;

        // "é" is two UTF-8 bytes; the prefix counts bytes, so it never collides with "e" + one byte.
        ConsentStreamId.Derive(ns, "café", "x").ShouldNotBe(ConsentStreamId.Derive(ns, "cafe", "x"));
        ConsentStreamId.Derive(ns, "café", "x").ShouldBe(ConsentStreamId.Derive(ns, "café", "x"));
    }

    [Fact]
    public void Derive_NullPart_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ConsentStreamId.Derive(ConsentStreamId.ConsentNamespace, "a", null!));
    }
}
