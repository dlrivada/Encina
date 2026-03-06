using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Marten;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Marten.GDPR;

/// <summary>
/// Guard clause tests for <see cref="CryptoShredderSerializer"/>.
/// Verifies null checks on constructor parameters.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class CryptoShredderSerializerGuardTests
{
    private readonly ISerializer _mockInner = Substitute.For<ISerializer>();
    private readonly ISubjectKeyProvider _mockKeyProvider = Substitute.For<ISubjectKeyProvider>();
    private readonly IForgottenSubjectHandler _mockForgottenHandler = Substitute.For<IForgottenSubjectHandler>();
    private readonly ILogger<CryptoShredderSerializer> _logger = NullLogger<CryptoShredderSerializer>.Instance;

    [Fact]
    public void Constructor_NullInnerSerializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShredderSerializer(null!, _mockKeyProvider, _mockForgottenHandler, _logger));
        ex.ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullSubjectKeyProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShredderSerializer(_mockInner, null!, _mockForgottenHandler, _logger));
        ex.ParamName.ShouldBe("subjectKeyProvider");
    }

    [Fact]
    public void Constructor_NullForgottenSubjectHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShredderSerializer(_mockInner, _mockKeyProvider, null!, _logger));
        ex.ParamName.ShouldBe("forgottenSubjectHandler");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShredderSerializer(_mockInner, _mockKeyProvider, _mockForgottenHandler, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullAnonymizedPlaceholder_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CryptoShredderSerializer(_mockInner, _mockKeyProvider, _mockForgottenHandler, _logger, null!));
        ex.ParamName.ShouldBe("anonymizedPlaceholder");
    }

    [Fact]
    public void Constructor_EmptyAnonymizedPlaceholder_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            new CryptoShredderSerializer(_mockInner, _mockKeyProvider, _mockForgottenHandler, _logger, ""));
        ex.ParamName.ShouldBe("anonymizedPlaceholder");
    }

    [Fact]
    public void Constructor_WhitespaceAnonymizedPlaceholder_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            new CryptoShredderSerializer(_mockInner, _mockKeyProvider, _mockForgottenHandler, _logger, "   "));
        ex.ParamName.ShouldBe("anonymizedPlaceholder");
    }
}
