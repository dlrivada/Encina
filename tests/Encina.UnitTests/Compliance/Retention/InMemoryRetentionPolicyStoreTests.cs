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
/// Unit tests for <see cref="InMemoryRetentionPolicyStore"/>.
/// </summary>
public class InMemoryRetentionPolicyStoreTests
{
    private readonly ILogger<InMemoryRetentionPolicyStore> _logger;
    private readonly InMemoryRetentionPolicyStore _store;

    public InMemoryRetentionPolicyStoreTests()
    {
        _logger = Substitute.For<ILogger<InMemoryRetentionPolicyStore>>();
        _store = new InMemoryRetentionPolicyStore(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryRetentionPolicyStore(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidPolicy_ShouldReturnRight()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));

        // Act
        var result = await _store.CreateAsync(policy);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidPolicy_ShouldIncrementCount()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));

        // Act
        await _store.CreateAsync(policy);

        // Assert
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ShouldReturnLeft()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        await _store.CreateAsync(policy);

        // Act — same object, same Id
        var result = await _store.CreateAsync(policy);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PolicyAlreadyExistsCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateCategory_ShouldReturnLeft()
    {
        // Arrange — two distinct policies with the same data category
        var policy1 = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        var policy2 = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 5));
        await _store.CreateAsync(policy1);

        // Act
        var result = await _store.CreateAsync(policy2);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PolicyAlreadyExistsCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateCategory_ShouldNotIncrementCount()
    {
        // Arrange
        var policy1 = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        var policy2 = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 5));
        await _store.CreateAsync(policy1);

        // Act
        await _store.CreateAsync(policy2);

        // Assert
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_NullPolicy_ShouldThrow()
    {
        // Act
        var act = async () => await _store.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_MultipleDistinctCategories_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.CreateAsync(RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7)));
        await _store.CreateAsync(RetentionPolicy.Create("session-logs", TimeSpan.FromDays(90)));
        await _store.CreateAsync(RetentionPolicy.Create("marketing-consent", TimeSpan.FromDays(365 * 3)));

        // Assert
        _store.Count.Should().Be(3);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingPolicy_ShouldReturnSome()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        await _store.CreateAsync(policy);

        // Act
        var result = await _store.GetByIdAsync(policy.Id);

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<RetentionPolicy>)result;
        option.IsSome.Should().BeTrue();
        var found = (RetentionPolicy)option;
        found.Id.Should().Be(policy.Id);
        found.DataCategory.Should().Be("financial-records");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingPolicy_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetByIdAsync("non-existing-id");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<RetentionPolicy>)result;
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

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_ExistingCategory_ShouldReturnSome()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        await _store.CreateAsync(policy);

        // Act
        var result = await _store.GetByCategoryAsync("financial-records");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<RetentionPolicy>)result;
        option.IsSome.Should().BeTrue();
        var found = (RetentionPolicy)option;
        found.DataCategory.Should().Be("financial-records");
        found.Id.Should().Be(policy.Id);
    }

    [Fact]
    public async Task GetByCategoryAsync_NonExistingCategory_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetByCategoryAsync("non-existing-category");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<RetentionPolicy>)result;
        option.IsNone.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByCategoryAsync_InvalidCategory_ShouldThrow(string? category)
    {
        // Act
        var act = async () => await _store.GetByCategoryAsync(category!);

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
    public async Task GetAllAsync_WithPolicies_ShouldReturnAll()
    {
        // Arrange
        await _store.CreateAsync(RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7)));
        await _store.CreateAsync(RetentionPolicy.Create("session-logs", TimeSpan.FromDays(90)));

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingPolicy_ShouldReturnRight()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        await _store.CreateAsync(policy);
        var updated = policy with { RetentionPeriod = TimeSpan.FromDays(365 * 5) };

        // Act
        var result = await _store.UpdateAsync(updated);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ExistingPolicy_ShouldPersistChanges()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        await _store.CreateAsync(policy);
        var updated = policy with { RetentionPeriod = TimeSpan.FromDays(365 * 5), AutoDelete = false };

        // Act
        await _store.UpdateAsync(updated);

        // Assert
        var found = await GetPolicy(policy.Id);
        found.RetentionPeriod.Should().Be(TimeSpan.FromDays(365 * 5));
        found.AutoDelete.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NonExistingPolicy_ShouldReturnLeft()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));

        // Act — never stored
        var result = await _store.UpdateAsync(policy);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PolicyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task UpdateAsync_NullPolicy_ShouldThrow()
    {
        // Act
        var act = async () => await _store.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingPolicy_ShouldReturnRight()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        await _store.CreateAsync(policy);

        // Act
        var result = await _store.DeleteAsync(policy.Id);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ExistingPolicy_ShouldDecrementCount()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        await _store.CreateAsync(policy);

        // Act
        await _store.DeleteAsync(policy.Id);

        // Assert
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_ExistingPolicy_ShouldNotBeRetrievable()
    {
        // Arrange
        var policy = RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7));
        await _store.CreateAsync(policy);

        // Act
        await _store.DeleteAsync(policy.Id);

        // Assert
        var result = await _store.GetByIdAsync(policy.Id);
        var option = (Option<RetentionPolicy>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingPolicy_ShouldReturnLeft()
    {
        // Act
        var result = await _store.DeleteAsync("non-existing-id");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PolicyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteAsync_InvalidId_ShouldThrow(string? id)
    {
        // Act
        var act = async () => await _store.DeleteAsync(id!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Testing Helpers Tests

    [Fact]
    public void Count_EmptyStore_ShouldReturnZero()
    {
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Count_AfterCreating_ShouldReflectStoredPolicies()
    {
        // Arrange & Act
        await _store.CreateAsync(RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7)));
        await _store.CreateAsync(RetentionPolicy.Create("session-logs", TimeSpan.FromDays(90)));

        // Assert
        _store.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetAllPolicies_ShouldReturnAllPolicies()
    {
        // Arrange
        await _store.CreateAsync(RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7)));
        await _store.CreateAsync(RetentionPolicy.Create("session-logs", TimeSpan.FromDays(90)));

        // Act
        var policies = _store.GetAllPolicies();

        // Assert
        policies.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllPolicies_ShouldReturnSnapshot_NotLiveView()
    {
        // Arrange
        await _store.CreateAsync(RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7)));
        var snapshot = _store.GetAllPolicies();

        // Act — add another policy after snapshot
        await _store.CreateAsync(RetentionPolicy.Create("session-logs", TimeSpan.FromDays(90)));

        // Assert — snapshot is unchanged
        snapshot.Should().HaveCount(1);
        _store.Count.Should().Be(2);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllPolicies()
    {
        // Arrange
        await _store.CreateAsync(RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7)));
        _store.Count.Should().Be(1);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Clear_AfterClear_GetAllPolicies_ShouldReturnEmpty()
    {
        // Arrange
        await _store.CreateAsync(RetentionPolicy.Create("financial-records", TimeSpan.FromDays(365 * 7)));

        // Act
        _store.Clear();
        var policies = _store.GetAllPolicies();

        // Assert
        policies.Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private async Task<RetentionPolicy> GetPolicy(string id)
    {
        var result = await _store.GetByIdAsync(id);
        var option = (Option<RetentionPolicy>)result;
        return (RetentionPolicy)option;
    }

    #endregion
}
