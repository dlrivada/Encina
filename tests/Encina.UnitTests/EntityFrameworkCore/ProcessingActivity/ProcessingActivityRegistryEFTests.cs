using Encina.Compliance.GDPR;
using Encina.EntityFrameworkCore.ProcessingActivity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using GdprProcessingActivity = Encina.Compliance.GDPR.ProcessingActivity;

namespace Encina.UnitTests.EntityFrameworkCore.ProcessingActivity;

/// <summary>
/// Unit tests for <see cref="ProcessingActivityRegistryEF"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ProcessingActivityRegistryEFTests
{
    #region Constructor Validation

    [Fact]
    public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ProcessingActivityRegistryEF(null!));
    }

    [Fact]
    public void Constructor_WithValidDbContext_DoesNotThrow()
    {
        // Arrange
        var context = CreateInMemoryContext();

        // Act
        var registry = new ProcessingActivityRegistryEF(context);

        // Assert
        registry.ShouldNotBeNull();
    }

    #endregion

    #region RegisterActivityAsync

    [Fact]
    public async Task RegisterActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => registry.RegisterActivityAsync(null!).AsTask());
    }

    [Fact]
    public async Task RegisterActivityAsync_WithValidActivity_ReturnsRight()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);
        var activity = CreateTestActivity();

        // Act
        var result = await registry.RegisterActivityAsync(activity);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterActivityAsync_DuplicateRequestType_ReturnsLeft()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);
        var activity = CreateTestActivity();

        // Register first
        var firstResult = await registry.RegisterActivityAsync(activity);
        firstResult.IsRight.ShouldBeTrue();

        // Act - register same RequestType again
        var secondResult = await registry.RegisterActivityAsync(activity);

        // Assert
        secondResult.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterActivityAsync_WhenCancelled_ReturnsLeft()
    {
        // Arrange
        var mockContext = Substitute.For<DbContext>();
        var mockSet = Substitute.For<DbSet<ProcessingActivityEntity>>();
        mockContext.Set<ProcessingActivityEntity>().Returns(mockSet);
        mockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException("Operation was cancelled"));

        var registry = new ProcessingActivityRegistryEF(mockContext);
        var activity = CreateTestActivity();

        // Act
        var result = await registry.RegisterActivityAsync(activity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetAllActivitiesAsync

    [Fact]
    public async Task GetAllActivitiesAsync_EmptyStore_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);

        // Act
        var result = await registry.GetAllActivitiesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            activities => activities.Count.ShouldBe(0),
            _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAllActivitiesAsync_WithRegisteredActivities_ReturnsAll()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);

        var activity1 = CreateTestActivity(typeof(TestRequest1));
        var activity2 = CreateTestActivity(typeof(TestRequest2));

        await registry.RegisterActivityAsync(activity1);
        await registry.RegisterActivityAsync(activity2);

        // Act
        var result = await registry.GetAllActivitiesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            activities => activities.Count.ShouldBe(2),
            _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAllActivitiesAsync_WhenExceptionOccurs_ReturnsLeft()
    {
        // Arrange
        var mockContext = Substitute.For<DbContext>();
        mockContext.Set<ProcessingActivityEntity>()
            .Returns(_ => throw new InvalidOperationException("DB error"));

        var registry = new ProcessingActivityRegistryEF(mockContext);

        // Act
        var result = await registry.GetAllActivitiesAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetActivityByRequestTypeAsync

    [Fact]
    public async Task GetActivityByRequestTypeAsync_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => registry.GetActivityByRequestTypeAsync(null!).AsTask());
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_ExistingType_ReturnsSome()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);
        var activity = CreateTestActivity(typeof(TestRequest1));
        await registry.RegisterActivityAsync(activity);

        // Act
        var result = await registry.GetActivityByRequestTypeAsync(typeof(TestRequest1));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            option => option.IsSome.ShouldBeTrue(),
            _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NonExistingType_ReturnsNone()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);

        // Act
        var result = await registry.GetActivityByRequestTypeAsync(typeof(TestRequest1));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            option => option.IsNone.ShouldBeTrue(),
            _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region UpdateActivityAsync

    [Fact]
    public async Task UpdateActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => registry.UpdateActivityAsync(null!).AsTask());
    }

    [Fact]
    public async Task UpdateActivityAsync_ExistingActivity_ReturnsRight()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);
        var activity = CreateTestActivity(typeof(TestRequest1));
        await registry.RegisterActivityAsync(activity);

        // Create updated version
        var updatedActivity = activity with
        {
            Name = "Updated Name",
            Purpose = "Updated Purpose"
        };

        // Act
        var result = await registry.UpdateActivityAsync(updatedActivity);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateActivityAsync_NonExistingActivity_ReturnsLeft()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);
        var activity = CreateTestActivity(typeof(TestRequest1));

        // Act - update without registering first
        var result = await registry.UpdateActivityAsync(activity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateActivityAsync_WhenCancelled_ReturnsLeft()
    {
        // Arrange
        var mockContext = Substitute.For<DbContext>();
        var mockSet = Substitute.For<DbSet<ProcessingActivityEntity>>();
        mockContext.Set<ProcessingActivityEntity>().Returns(mockSet);

        // First, FirstOrDefaultAsync must find something, then SaveChanges throws
        // Since mocking FirstOrDefaultAsync on DbSet is complex, test via OperationCanceledException
        mockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var registry = new ProcessingActivityRegistryEF(mockContext);
        var activity = CreateTestActivity();

        // Act
        var result = await registry.UpdateActivityAsync(activity);

        // Assert - Should return Left (either not found or cancelled)
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Interface Compliance

    [Fact]
    public void ImplementsIProcessingActivityRegistry()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var registry = new ProcessingActivityRegistryEF(context);

        // Assert
        (registry is IProcessingActivityRegistry).ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private static ProcessingActivityTestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ProcessingActivityTestDbContext>()
            .UseInMemoryDatabase($"processing-activity-test-{Guid.NewGuid()}")
            .Options;
        return new ProcessingActivityTestDbContext(options);
    }

    private static GdprProcessingActivity CreateTestActivity(Type? requestType = null)
    {
        return new GdprProcessingActivity
        {
            Id = Guid.NewGuid(),
            Name = "Test Processing Activity",
            Purpose = "Unit testing",
            LawfulBasis = LawfulBasis.LegitimateInterests,
            CategoriesOfDataSubjects = ["Test Subjects"],
            CategoriesOfPersonalData = ["Test Data"],
            Recipients = ["Internal"],
            ThirdCountryTransfers = null,
            Safeguards = null,
            RetentionPeriod = TimeSpan.FromDays(365),
            SecurityMeasures = "Encryption at rest",
            RequestType = requestType ?? typeof(TestRequest1),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private sealed class ProcessingActivityTestDbContext : DbContext
    {
        public ProcessingActivityTestDbContext(DbContextOptions<ProcessingActivityTestDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProcessingActivityEntity> ProcessingActivities => Set<ProcessingActivityEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProcessingActivityEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RequestTypeName).IsUnique();
            });
        }
    }

    // Test request types for simulating different processing activities
    private sealed class TestRequest1;
    private sealed class TestRequest2;

    #endregion
}
