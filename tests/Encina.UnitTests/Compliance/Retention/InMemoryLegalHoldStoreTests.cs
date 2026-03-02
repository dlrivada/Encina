using Encina.Compliance.Retention;
using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="InMemoryLegalHoldStore"/>.
/// </summary>
public class InMemoryLegalHoldStoreTests
{
    private readonly ILogger<InMemoryLegalHoldStore> _logger;
    private readonly InMemoryLegalHoldStore _store;

    public InMemoryLegalHoldStoreTests()
    {
        _logger = Substitute.For<ILogger<InMemoryLegalHoldStore>>();
        _store = new InMemoryLegalHoldStore(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryLegalHoldStore(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidHold_ShouldReturnRight()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Pending tax audit");

        // Act
        var result = await _store.CreateAsync(hold);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidHold_ShouldIncrementCount()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Pending tax audit");

        // Act
        await _store.CreateAsync(hold);

        // Assert
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ShouldReturnLeft()
    {
        // Arrange — same object, same Id
        var hold = LegalHold.Create("entity-1", "Pending tax audit");
        await _store.CreateAsync(hold);

        // Act
        var result = await _store.CreateAsync(hold);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldAlreadyActiveCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ShouldNotIncrementCount()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Pending tax audit");
        await _store.CreateAsync(hold);

        // Act
        await _store.CreateAsync(hold);

        // Assert
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_NullHold_ShouldThrow()
    {
        // Act
        var act = async () => await _store.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_MultipleHoldsForDifferentEntities_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.CreateAsync(LegalHold.Create("entity-1", "Tax audit"));
        await _store.CreateAsync(LegalHold.Create("entity-2", "Litigation hold"));
        await _store.CreateAsync(LegalHold.Create("entity-3", "Regulatory investigation"));

        // Assert
        _store.Count.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_MultipleDistinctHoldsForSameEntity_ShouldStoreAll()
    {
        // Arrange — different hold IDs for the same entity
        var hold1 = LegalHold.Create("entity-1", "Tax audit");
        var hold2 = LegalHold.Create("entity-1", "Litigation hold");

        // Act
        await _store.CreateAsync(hold1);
        var result = await _store.CreateAsync(hold2);

        // Assert — different Ids, both should be stored
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(2);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingHold_ShouldReturnSome()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Pending tax audit", "legal-counsel@company.com");
        await _store.CreateAsync(hold);

        // Act
        var result = await _store.GetByIdAsync(hold.Id);

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<LegalHold>)result;
        option.IsSome.Should().BeTrue();
        var found = (LegalHold)option;
        found.Id.Should().Be(hold.Id);
        found.EntityId.Should().Be("entity-1");
        found.Reason.Should().Be("Pending tax audit");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingHold_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetByIdAsync("non-existing-id");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<LegalHold>)result;
        option.IsNone.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_InvalidId_ShouldThrow(string? id)
    {
        // Act
        var act = async () => await _store.GetByIdAsync(id!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetByEntityIdAsync Tests

    [Fact]
    public async Task GetByEntityIdAsync_ExistingEntity_ShouldReturnMatchingHolds()
    {
        // Arrange
        await _store.CreateAsync(LegalHold.Create("entity-1", "Tax audit"));
        await _store.CreateAsync(LegalHold.Create("entity-1", "Litigation hold"));
        await _store.CreateAsync(LegalHold.Create("entity-2", "Regulatory hold"));

        // Act
        var result = await _store.GetByEntityIdAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
        list.Should().OnlyContain(h => h.EntityId == "entity-1");
    }

    [Fact]
    public async Task GetByEntityIdAsync_NonExistingEntity_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetByEntityIdAsync("non-existing-entity");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByEntityIdAsync_InvalidEntityId_ShouldThrow(string? entityId)
    {
        // Act
        var act = async () => await _store.GetByEntityIdAsync(entityId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsUnderHoldAsync Tests

    [Fact]
    public async Task IsUnderHoldAsync_EntityWithActiveHold_ShouldReturnTrue()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);

        // Act
        var result = await _store.IsUnderHoldAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var isHeld = (bool)result;
        isHeld.Should().BeTrue();
    }

    [Fact]
    public async Task IsUnderHoldAsync_EntityWithReleasedHold_ShouldReturnFalse()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);
        await _store.ReleaseAsync(hold.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Act
        var result = await _store.IsUnderHoldAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var isHeld = (bool)result;
        isHeld.Should().BeFalse();
    }

    [Fact]
    public async Task IsUnderHoldAsync_EntityWithNoHold_ShouldReturnFalse()
    {
        // Act
        var result = await _store.IsUnderHoldAsync("entity-no-hold");

        // Assert
        result.IsRight.Should().BeTrue();
        var isHeld = (bool)result;
        isHeld.Should().BeFalse();
    }

    [Fact]
    public async Task IsUnderHoldAsync_EntityWithMultipleHoldsOneActive_ShouldReturnTrue()
    {
        // Arrange — one released, one still active
        var hold1 = LegalHold.Create("entity-1", "Tax audit");
        var hold2 = LegalHold.Create("entity-1", "Litigation hold");
        await _store.CreateAsync(hold1);
        await _store.CreateAsync(hold2);
        await _store.ReleaseAsync(hold1.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Act
        var result = await _store.IsUnderHoldAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var isHeld = (bool)result;
        isHeld.Should().BeTrue();
    }

    [Fact]
    public async Task IsUnderHoldAsync_DifferentEntity_ShouldReturnFalse()
    {
        // Arrange
        await _store.CreateAsync(LegalHold.Create("entity-1", "Tax audit"));

        // Act — query a different entity
        var result = await _store.IsUnderHoldAsync("entity-2");

        // Assert
        result.IsRight.Should().BeTrue();
        var isHeld = (bool)result;
        isHeld.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsUnderHoldAsync_InvalidEntityId_ShouldThrow(string? entityId)
    {
        // Act
        var act = async () => await _store.IsUnderHoldAsync(entityId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetActiveHoldsAsync Tests

    [Fact]
    public async Task GetActiveHoldsAsync_WithActiveHolds_ShouldReturnOnlyActive()
    {
        // Arrange
        var hold1 = LegalHold.Create("entity-1", "Tax audit");
        var hold2 = LegalHold.Create("entity-2", "Litigation");
        await _store.CreateAsync(hold1);
        await _store.CreateAsync(hold2);
        await _store.ReleaseAsync(hold1.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Act
        var result = await _store.GetActiveHoldsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(1);
        list[0].EntityId.Should().Be("entity-2");
    }

    [Fact]
    public async Task GetActiveHoldsAsync_AllReleased_ShouldReturnEmpty()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);
        await _store.ReleaseAsync(hold.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Act
        var result = await _store.GetActiveHoldsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveHoldsAsync_EmptyStore_ShouldReturnEmpty()
    {
        // Act
        var result = await _store.GetActiveHoldsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveHoldsAsync_AllActive_ShouldReturnAll()
    {
        // Arrange
        await _store.CreateAsync(LegalHold.Create("entity-1", "Tax audit"));
        await _store.CreateAsync(LegalHold.Create("entity-2", "Litigation"));

        // Act
        var result = await _store.GetActiveHoldsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
        list.Should().OnlyContain(h => h.IsActive);
    }

    #endregion

    #region ReleaseAsync Tests

    [Fact]
    public async Task ReleaseAsync_ActiveHold_ShouldReturnRight()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);

        // Act
        var result = await _store.ReleaseAsync(hold.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseAsync_ActiveHold_ShouldSetReleasedAtUtc()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);
        var releasedAt = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
        await _store.ReleaseAsync(hold.Id, "legal-counsel", releasedAt);

        // Assert
        var updated = await GetHold(hold.Id);
        updated.ReleasedAtUtc.Should().Be(releasedAt);
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_ActiveHold_ShouldSetReleasedByUserId()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);

        // Act
        await _store.ReleaseAsync(hold.Id, "legal-counsel@company.com", DateTimeOffset.UtcNow);

        // Assert
        var updated = await GetHold(hold.Id);
        updated.ReleasedByUserId.Should().Be("legal-counsel@company.com");
    }

    [Fact]
    public async Task ReleaseAsync_ActiveHold_WithNullReleasedByUserId_ShouldSucceed()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);

        // Act
        var result = await _store.ReleaseAsync(hold.Id, null, DateTimeOffset.UtcNow);

        // Assert
        result.IsRight.Should().BeTrue();
        var updated = await GetHold(hold.Id);
        updated.ReleasedByUserId.Should().BeNull();
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_NonExistingHold_ShouldReturnLeft()
    {
        // Act
        var result = await _store.ReleaseAsync("non-existing-id", "legal-counsel", DateTimeOffset.UtcNow);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task ReleaseAsync_AlreadyReleasedHold_ShouldReturnLeft()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);
        await _store.ReleaseAsync(hold.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Act — try to release again
        var result = await _store.ReleaseAsync(hold.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldAlreadyReleasedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReleaseAsync_InvalidHoldId_ShouldThrow(string? holdId)
    {
        // Act
        var act = async () => await _store.ReleaseAsync(holdId!, "legal-counsel", DateTimeOffset.UtcNow);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyStore_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithHolds_ShouldReturnAll()
    {
        // Arrange
        await _store.CreateAsync(LegalHold.Create("entity-1", "Tax audit"));
        await _store.CreateAsync(LegalHold.Create("entity-2", "Litigation"));

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_IncludesBothActiveAndReleasedHolds()
    {
        // Arrange
        var hold1 = LegalHold.Create("entity-1", "Tax audit");
        var hold2 = LegalHold.Create("entity-2", "Litigation");
        await _store.CreateAsync(hold1);
        await _store.CreateAsync(hold2);
        await _store.ReleaseAsync(hold1.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
    }

    #endregion

    #region Testing Helpers Tests

    [Fact]
    public void Count_EmptyStore_ShouldReturnZero()
    {
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Count_AfterCreating_ShouldReflectStoredHolds()
    {
        // Arrange & Act
        await _store.CreateAsync(LegalHold.Create("entity-1", "Tax audit"));
        await _store.CreateAsync(LegalHold.Create("entity-2", "Litigation"));

        // Assert
        _store.Count.Should().Be(2);
    }

    [Fact]
    public async Task Count_AfterRelease_ShouldRemainUnchanged()
    {
        // Arrange — release does NOT remove the hold, it just marks it released
        var hold = LegalHold.Create("entity-1", "Tax audit");
        await _store.CreateAsync(hold);

        // Act
        await _store.ReleaseAsync(hold.Id, "legal-counsel", DateTimeOffset.UtcNow);

        // Assert
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllHolds()
    {
        // Arrange
        await _store.CreateAsync(LegalHold.Create("entity-1", "Tax audit"));
        _store.Count.Should().Be(1);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Clear_AfterClear_GetAllAsync_ShouldReturnEmpty()
    {
        // Arrange
        await _store.CreateAsync(LegalHold.Create("entity-1", "Tax audit"));

        // Act
        _store.Clear();
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private async Task<LegalHold> GetHold(string id)
    {
        var result = await _store.GetByIdAsync(id);
        var option = (Option<LegalHold>)result;
        return (LegalHold)option;
    }

    #endregion
}
