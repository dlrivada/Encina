using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Marten.GDPR;

/// <summary>
/// Guard clause tests for <see cref="CryptoShredErasureStrategy"/>.
/// Verifies null checks on constructor parameters and public methods.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class CryptoShredErasureStrategyGuardTests
{
    private readonly ISubjectKeyProvider _mockKeyProvider = Substitute.For<ISubjectKeyProvider>();
    private readonly ILogger<CryptoShredErasureStrategy> _logger = NullLogger<CryptoShredErasureStrategy>.Instance;

    [Fact]
    public void Constructor_NullSubjectKeyProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShredErasureStrategy(null!, _logger));
        ex.ParamName.ShouldBe("subjectKeyProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShredErasureStrategy(_mockKeyProvider, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task EraseFieldAsync_NullLocation_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new CryptoShredErasureStrategy(_mockKeyProvider, _logger);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            sut.EraseFieldAsync(null!).AsTask());
        ex.ParamName.ShouldBe("location");
    }
}
