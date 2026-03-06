using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class CryptoShredErasureStrategyTests
{
    private readonly ISubjectKeyProvider _mockKeyProvider = Substitute.For<ISubjectKeyProvider>();
    private readonly CryptoShredErasureStrategy _sut;

    public CryptoShredErasureStrategyTests()
    {
        _sut = new CryptoShredErasureStrategy(
            _mockKeyProvider,
            NullLogger<CryptoShredErasureStrategy>.Instance);
    }

    [Fact]
    public async Task EraseFieldAsync_DelegatesDeleteToKeyProvider()
    {
        // Arrange
        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = "user-42",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        var shreddingResult = new CryptoShreddingResult
        {
            SubjectId = "user-42",
            KeysDeleted = 1,
            FieldsAffected = 0,
            ShreddedAtUtc = DateTimeOffset.UtcNow
        };

        _mockKeyProvider
            .DeleteSubjectKeysAsync("user-42", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, CryptoShreddingResult>(shreddingResult));

        // Act
        var result = await _sut.EraseFieldAsync(location);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _mockKeyProvider.Received(1)
            .DeleteSubjectKeysAsync("user-42", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EraseFieldAsync_WhenKeyProviderFails_ReturnsError()
    {
        // Arrange
        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = "user-42",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        var error = CryptoShreddingErrors.SubjectForgotten("user-42");
        _mockKeyProvider
            .DeleteSubjectKeysAsync("user-42", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, CryptoShreddingResult>(error));

        // Act
        var result = await _sut.EraseFieldAsync(location);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task EraseFieldAsync_ExtractsSubjectIdFromEntityId()
    {
        // Arrange
        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = "subject-abc-123",
            FieldName = "Name",
            Category = PersonalDataCategory.Other,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        var shreddingResult = new CryptoShreddingResult
        {
            SubjectId = "subject-abc-123",
            KeysDeleted = 2,
            FieldsAffected = 0,
            ShreddedAtUtc = DateTimeOffset.UtcNow
        };

        _mockKeyProvider
            .DeleteSubjectKeysAsync("subject-abc-123", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, CryptoShreddingResult>(shreddingResult));

        // Act
        var result = await _sut.EraseFieldAsync(location);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _mockKeyProvider.Received(1)
            .DeleteSubjectKeysAsync("subject-abc-123", Arg.Any<CancellationToken>());
    }
}
