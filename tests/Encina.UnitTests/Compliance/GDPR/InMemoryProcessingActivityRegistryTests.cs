using Encina.Compliance.GDPR;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="InMemoryProcessingActivityRegistry"/>.
/// </summary>
public class InMemoryProcessingActivityRegistryTests
{
    private readonly InMemoryProcessingActivityRegistry _sut = new();

    private static ProcessingActivity CreateActivity(Type? requestType = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Activity",
        Purpose = "Unit testing",
        LawfulBasis = LawfulBasis.Contract,
        CategoriesOfDataSubjects = ["Users"],
        CategoriesOfPersonalData = ["Email"],
        Recipients = ["Internal"],
        RetentionPeriod = TimeSpan.FromDays(365),
        SecurityMeasures = "Encryption",
        RequestType = requestType ?? typeof(InMemoryProcessingActivityRegistryTests),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };

    // -- RegisterActivityAsync --

    [Fact]
    public async Task RegisterActivityAsync_ValidActivity_ShouldSucceed()
    {
        // Arrange
        var activity = CreateActivity();

        // Act
        var result = await _sut.RegisterActivityAsync(activity);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterActivityAsync_DuplicateRequestType_ShouldReturnError()
    {
        // Arrange
        var activity1 = CreateActivity(typeof(string));
        var activity2 = CreateActivity(typeof(string));
        await _sut.RegisterActivityAsync(activity1);

        // Act
        var result = await _sut.RegisterActivityAsync(activity2);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("already registered");
    }

    [Fact]
    public async Task RegisterActivityAsync_NullActivity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.RegisterActivityAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("activity");
    }

    // -- GetAllActivitiesAsync --

    [Fact]
    public async Task GetAllActivitiesAsync_EmptyRegistry_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetAllActivitiesAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var activities = result.Match(Right: a => a, Left: _ => (IReadOnlyList<ProcessingActivity>)[]);
        activities.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_WithActivities_ShouldReturnAll()
    {
        // Arrange
        await _sut.RegisterActivityAsync(CreateActivity(typeof(string)));
        await _sut.RegisterActivityAsync(CreateActivity(typeof(int)));

        // Act
        var result = await _sut.GetAllActivitiesAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var activities = result.Match(Right: a => a, Left: _ => (IReadOnlyList<ProcessingActivity>)[]);
        activities.Should().HaveCount(2);
    }

    // -- GetActivityByRequestTypeAsync --

    [Fact]
    public async Task GetActivityByRequestTypeAsync_Registered_ShouldReturnActivity()
    {
        // Arrange
        var activity = CreateActivity(typeof(string));
        await _sut.RegisterActivityAsync(activity);

        // Act
        var result = await _sut.GetActivityByRequestTypeAsync(typeof(string));

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<ProcessingActivity>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(found => found.Name.Should().Be("Test Activity"));
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NotRegistered_ShouldReturnNone()
    {
        // Act
        var result = await _sut.GetActivityByRequestTypeAsync(typeof(string));

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<ProcessingActivity>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NullRequestType_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.GetActivityByRequestTypeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("requestType");
    }

    // -- UpdateActivityAsync --

    [Fact]
    public async Task UpdateActivityAsync_Existing_ShouldSucceed()
    {
        // Arrange
        var activity = CreateActivity(typeof(string));
        await _sut.RegisterActivityAsync(activity);

        var updated = activity with { Purpose = "Updated purpose" };

        // Act
        var result = await _sut.UpdateActivityAsync(updated);

        // Assert
        result.IsRight.Should().BeTrue();

        var retrieved = await _sut.GetActivityByRequestTypeAsync(typeof(string));
        var option = (Option<ProcessingActivity>)retrieved;
        option.IsSome.Should().BeTrue();
        option.IfSome(found => found.Purpose.Should().Be("Updated purpose"));
    }

    [Fact]
    public async Task UpdateActivityAsync_NotRegistered_ShouldReturnError()
    {
        // Arrange
        var activity = CreateActivity(typeof(string));

        // Act
        var result = await _sut.UpdateActivityAsync(activity);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("No processing activity is registered");
    }

    [Fact]
    public async Task UpdateActivityAsync_NullActivity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.UpdateActivityAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("activity");
    }

    // -- AutoRegisterFromAssemblies --

    [Fact]
    public void AutoRegisterFromAssemblies_NullAssemblies_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _sut.AutoRegisterFromAssemblies(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AutoRegisterFromAssemblies_AssemblyWithAttributes_ShouldRegisterActivities()
    {
        // Arrange — this test assembly contains SampleDecoratedRequest
        var assemblies = new[] { typeof(SampleDecoratedRequest).Assembly };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Act
        var count = _sut.AutoRegisterFromAssemblies(assemblies, timeProvider);

        // Assert
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AutoRegisterFromAssemblies_DuplicateCall_ShouldSkipExisting()
    {
        // Arrange
        var assemblies = new[] { typeof(SampleDecoratedRequest).Assembly };

        // Act
        var first = _sut.AutoRegisterFromAssemblies(assemblies);
        var second = _sut.AutoRegisterFromAssemblies(assemblies);

        // Assert
        first.Should().BeGreaterThan(0);
        second.Should().Be(0);
    }

    [Fact]
    public void AutoRegisterFromAssemblies_EmptyAssemblyList_ShouldRegisterZero()
    {
        // Act
        var count = _sut.AutoRegisterFromAssemblies([]);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void AutoRegisterFromAssemblies_WithTimeProvider_ShouldUseProvidedTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var assemblies = new[] { typeof(SampleDecoratedRequest).Assembly };

        // Act
        _sut.AutoRegisterFromAssemblies(assemblies, timeProvider);

        // Assert
        var result = _sut.GetActivityByRequestTypeAsync(typeof(SampleDecoratedRequest)).AsTask().Result;
        var option = (Option<ProcessingActivity>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(activity => activity.CreatedAtUtc.Should().Be(fixedTime));
    }
}

// Test stub types used across GDPR tests

[ProcessingActivity(
    Purpose = "Test processing",
    LawfulBasis = LawfulBasis.Contract,
    DataCategories = ["Email"],
    DataSubjects = ["Users"],
    RetentionDays = 365)]
public sealed record SampleDecoratedRequest : ICommand<Unit>;

[ProcessesPersonalData]
public sealed record SampleMarkerOnlyRequest : ICommand<Unit>;

public sealed record SampleNoAttributeRequest : ICommand<Unit>;
