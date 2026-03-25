using System.Security.Cryptography;

using Encina.Compliance.Attestation;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Attestation;

public class ContentHasherTests
{
    [Fact]
    public void ComputeSha256_ShouldReturnDeterministicHash()
    {
        var hash1 = ContentHasher.ComputeSha256("hello world");
        var hash2 = ContentHasher.ComputeSha256("hello world");

        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ComputeSha256_DifferentInput_ShouldReturnDifferentHash()
    {
        var hash1 = ContentHasher.ComputeSha256("hello world");
        var hash2 = ContentHasher.ComputeSha256("goodbye world");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeSha256_NullContent_ThrowsArgumentNullException()
    {
        var act = () => ContentHasher.ComputeSha256(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeSha256_ReturnsLowercaseHex()
    {
        var hash = ContentHasher.ComputeSha256("test");

        hash.Should().MatchRegex("^[0-9a-f]+$");
        hash.Should().HaveLength(64); // SHA-256 = 32 bytes = 64 hex chars
    }

    [Fact]
    public void ComputeHash_SHA256_ShouldMatchComputeSha256()
    {
        var hash1 = ContentHasher.ComputeSha256("test data");
        var hash2 = ContentHasher.ComputeHash("test data", HashAlgorithmName.SHA256);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_SHA384_ShouldReturn96HexChars()
    {
        var hash = ContentHasher.ComputeHash("test data", HashAlgorithmName.SHA384);

        hash.Should().HaveLength(96); // SHA-384 = 48 bytes = 96 hex chars
    }

    [Fact]
    public void ComputeHash_SHA512_ShouldReturn128HexChars()
    {
        var hash = ContentHasher.ComputeHash("test data", HashAlgorithmName.SHA512);

        hash.Should().HaveLength(128); // SHA-512 = 64 bytes = 128 hex chars
    }

    [Fact]
    public void ComputeHash_UnsupportedAlgorithm_ThrowsArgumentException()
    {
        var act = () => ContentHasher.ComputeHash("test", HashAlgorithmName.MD5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ComputeHash_NullContent_ThrowsArgumentNullException()
    {
        var act = () => ContentHasher.ComputeHash(null!, HashAlgorithmName.SHA256);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateHmac_SHA256_ReturnsHMACSHA256()
    {
        var key = new byte[32];
        using var hmac = ContentHasher.CreateHmac(key, HashAlgorithmName.SHA256);

        hmac.Should().BeOfType<HMACSHA256>();
    }

    [Fact]
    public void CreateHmac_SHA384_ReturnsHMACSHA384()
    {
        var key = new byte[48];
        using var hmac = ContentHasher.CreateHmac(key, HashAlgorithmName.SHA384);

        hmac.Should().BeOfType<HMACSHA384>();
    }

    [Fact]
    public void CreateHmac_SHA512_ReturnsHMACSHA512()
    {
        var key = new byte[64];
        using var hmac = ContentHasher.CreateHmac(key, HashAlgorithmName.SHA512);

        hmac.Should().BeOfType<HMACSHA512>();
    }

    [Fact]
    public void CreateHmac_UnsupportedAlgorithm_ThrowsArgumentException()
    {
        var key = new byte[32];
        var act = () => ContentHasher.CreateHmac(key, HashAlgorithmName.MD5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetRecommendedKeySize_SHA256_Returns32()
    {
        ContentHasher.GetRecommendedKeySize(HashAlgorithmName.SHA256).Should().Be(32);
    }

    [Fact]
    public void GetRecommendedKeySize_SHA384_Returns48()
    {
        ContentHasher.GetRecommendedKeySize(HashAlgorithmName.SHA384).Should().Be(48);
    }

    [Fact]
    public void GetRecommendedKeySize_SHA512_Returns64()
    {
        ContentHasher.GetRecommendedKeySize(HashAlgorithmName.SHA512).Should().Be(64);
    }

    [Fact]
    public void GetRecommendedKeySize_Unknown_ReturnsFallback32()
    {
        ContentHasher.GetRecommendedKeySize(HashAlgorithmName.MD5).Should().Be(32);
    }
}
