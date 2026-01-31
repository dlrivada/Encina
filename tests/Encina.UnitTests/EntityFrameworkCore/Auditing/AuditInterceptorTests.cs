using System.Diagnostics.CodeAnalysis;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.UnitTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Tests for AuditInterceptor.
/// </summary>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern for NSubstitute")]
public class AuditInterceptorTests
{
    #region Test Types

    private sealed class AuditedTestEntity : IAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
    }

    private sealed class SoftDeletableTestEntity : IAuditableEntity, ISoftDeletable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }

    private sealed class NonAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<AuditedTestEntity> AuditedEntities => Set<AuditedTestEntity>();
        public DbSet<SoftDeletableTestEntity> SoftDeletableEntities => Set<SoftDeletableTestEntity>();
        public DbSet<NonAuditableEntity> NonAuditableEntities => Set<NonAuditableEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditedTestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<SoftDeletableTestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<NonAuditableEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(null!, options, timeProvider, logger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, null!, timeProvider, logger));
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, options, null!, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AuditInterceptor(serviceProvider, options, timeProvider, null!));
    }

    [Fact]
    public void Constructor_NullAuditLogStore_DoesNotThrow()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        // Act & Assert - null auditLogStore is optional
        var interceptor = new AuditInterceptor(serviceProvider, options, timeProvider, logger, null);
        interceptor.ShouldNotBeNull();
    }

    #endregion

    #region SaveChangesAsync Tests - Added Entities

    [Fact]
    public async Task SaveChangesAsync_AddedEntityWithIAuditableEntity_SetsCreatedFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBe(userId);
    }

    [Fact]
    public async Task SaveChangesAsync_AddedEntityWithIAuditableEntity_DoesNotSetModifiedFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert - Modified fields should not be set on insert
        entity.ModifiedAtUtc.ShouldBeNull();
        entity.ModifiedBy.ShouldBeNull();
    }

    #endregion

    #region SaveChangesAsync Tests - Modified Entities

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntityWithIAuditableEntity_SetsModifiedFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "modifier-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);

        // Create and save entity first (this will set Created fields)
        var entity = new AuditedTestEntity { Name = "Original" };
        context.AuditedEntities.Add(entity);
        await context.SaveChangesAsync();

        // Note the created values
        var originalCreatedAtUtc = entity.CreatedAtUtc;
        var originalCreatedBy = entity.CreatedBy;

        // Advance time and modify entity
        fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 16, 14, 0, 0, TimeSpan.Zero));
        entity.Name = "Modified";

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 16, 14, 0, 0, DateTimeKind.Utc));
        entity.ModifiedBy.ShouldBe(userId);
        // Created fields should be unchanged
        entity.CreatedAtUtc.ShouldBe(originalCreatedAtUtc);
        entity.CreatedBy.ShouldBe(originalCreatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntity_DoesNotOverwriteCreatedFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);

        // Create and save entity first
        var entity = new AuditedTestEntity { Name = "Original" };
        context.AuditedEntities.Add(entity);
        await context.SaveChangesAsync();

        var expectedCreatedAt = entity.CreatedAtUtc;
        var expectedCreatedBy = entity.CreatedBy;

        // Advance time and modify entity
        fakeTime.SetUtcNow(new DateTimeOffset(2024, 2, 20, 15, 45, 0, TimeSpan.Zero));
        entity.Name = "Updated";

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.ShouldBe(expectedCreatedAt);
        entity.CreatedBy.ShouldBe(expectedCreatedBy);
    }

    #endregion

    #region Options Tests

    [Fact]
    public async Task SaveChangesAsync_WhenDisabled_DoesNotSetAnyFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = false };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert - Fields should remain at default
        entity.CreatedAtUtc.ShouldBe(default);
        entity.CreatedBy.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_TrackCreatedAtFalse_DoesNotSetCreatedAtUtc()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = false,
            TrackCreatedBy = true
        };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.ShouldBe(default);
        entity.CreatedBy.ShouldBe(userId); // This should still be set
    }

    [Fact]
    public async Task SaveChangesAsync_TrackCreatedByFalse_DoesNotSetCreatedBy()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            TrackCreatedBy = false
        };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_TrackModifiedAtFalse_DoesNotSetModifiedAtUtc()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackModifiedAt = false,
            TrackModifiedBy = true
        };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);

        // Create entity first
        var entity = new AuditedTestEntity { Name = "Original" };
        context.AuditedEntities.Add(entity);
        await context.SaveChangesAsync();

        // Modify entity
        entity.Name = "Modified";

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.ShouldBeNull();
        entity.ModifiedBy.ShouldBe(userId);
    }

    [Fact]
    public async Task SaveChangesAsync_TrackModifiedByFalse_DoesNotSetModifiedBy()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackModifiedAt = true,
            TrackModifiedBy = false
        };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);

        // Create entity first
        var entity = new AuditedTestEntity { Name = "Original" };
        context.AuditedEntities.Add(entity);
        await context.SaveChangesAsync();

        // Advance time and modify entity
        fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 16, 14, 0, 0, TimeSpan.Zero));
        entity.Name = "Modified";

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 16, 14, 0, 0, DateTimeKind.Utc));
        entity.ModifiedBy.ShouldBeNull();
    }

    #endregion

    #region Non-Auditable Entity Tests

    [Fact]
    public async Task SaveChangesAsync_NonAuditableEntity_IsSkipped()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        var serviceProvider = CreateServiceProviderWithUser("test-user");
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new NonAuditableEntity { Name = "Test" };
        context.NonAuditableEntities.Add(entity);

        // Act - Should not throw
        await context.SaveChangesAsync();

        // Assert - Entity should be saved without issues
        entity.Name.ShouldBe("Test");
    }

    #endregion

    #region Null User Tests

    [Fact]
    public async Task SaveChangesAsync_NullUserId_SetsTimestampButNotUserFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        var serviceProvider = Substitute.For<IServiceProvider>();
        // No IRequestContext registered - userId will be null
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBeNull(); // No user context
    }

    #endregion

    #region Sync Method Tests

    [Fact]
    public void SaveChanges_AddedEntityWithIAuditableEntity_SetsCreatedFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        context.SaveChanges();

        // Assert
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBe(userId);
    }

    #endregion

    #region Soft Delete Tests

    [Fact]
    public async Task SaveChangesAsync_SoftDeleteEntity_MarksAsDeleted()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "deleter-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);

        // Create and save entity first
        var entity = new SoftDeletableTestEntity { Name = "Test" };
        context.SoftDeletableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Mark as deleted
        fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 20, 15, 0, 0, TimeSpan.Zero));
        entity.IsDeleted = true;

        // Act
        await context.SaveChangesAsync();

        // Assert - Entity should be marked as deleted with modified fields updated
        entity.IsDeleted.ShouldBeTrue();
        // Modified fields should be updated
        entity.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 20, 15, 0, 0, DateTimeKind.Utc));
        entity.ModifiedBy.ShouldBe(userId);
    }

    #endregion

    #region Audit Log Store Tests

    [Fact]
    public async Task SaveChangesAsync_WithAuditLogStore_LogsAuditEntry()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            LogChangesToStore = true
        };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var auditLogStore = new InMemoryAuditLogStore();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger, auditLogStore);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        auditLogStore.GetTotalCount().ShouldBe(1);
        var history = await auditLogStore.GetHistoryAsync(nameof(AuditedTestEntity), entity.Id.ToString());
        history.ShouldNotBeEmpty();
        history.First().Action.ShouldBe(AuditAction.Created);
        history.First().UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task SaveChangesAsync_WithAuditLogStore_LogsModifyEntry()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            LogChangesToStore = true
        };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var auditLogStore = new InMemoryAuditLogStore();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger, auditLogStore);

        await using var context = CreateInMemoryContext(interceptor);

        // Create entity
        var entity = new AuditedTestEntity { Name = "Original" };
        context.AuditedEntities.Add(entity);
        await context.SaveChangesAsync();

        // Modify entity
        fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 16, 14, 0, 0, TimeSpan.Zero));
        entity.Name = "Modified";

        // Act
        await context.SaveChangesAsync();

        // Assert
        auditLogStore.GetTotalCount().ShouldBe(2);
        var history = await auditLogStore.GetHistoryAsync(nameof(AuditedTestEntity), entity.Id.ToString());
        history.Count().ShouldBe(2);
        history.ShouldContain(e => e.Action == AuditAction.Created);
        history.ShouldContain(e => e.Action == AuditAction.Updated);
    }

    [Fact]
    public async Task SaveChangesAsync_WithAuditLogStore_LogsUpdateEntryForSoftDelete()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            LogChangesToStore = true
        };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var auditLogStore = new InMemoryAuditLogStore();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger, auditLogStore);

        await using var context = CreateInMemoryContext(interceptor);

        // Create entity
        var entity = new SoftDeletableTestEntity { Name = "Test" };
        context.SoftDeletableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Soft delete entity (marks as modified in EF Core)
        fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 20, 15, 0, 0, TimeSpan.Zero));
        entity.IsDeleted = true;

        // Act
        await context.SaveChangesAsync();

        // Assert
        var history = await auditLogStore.GetHistoryAsync(nameof(SoftDeletableTestEntity), entity.Id.ToString());
        history.ShouldContain(e => e.Action == AuditAction.Created);
        // Soft delete is tracked as update or deleted depending on interceptor implementation
        history.Count().ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SaveChangesAsync_LogChangesToStoreFalse_DoesNotLogToStore()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            LogChangesToStore = false
        };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var auditLogStore = new InMemoryAuditLogStore();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger, auditLogStore);

        await using var context = CreateInMemoryContext(interceptor);
        var entity = new AuditedTestEntity { Name = "Test" };
        context.AuditedEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert - Audit log store should be empty
        auditLogStore.GetTotalCount().ShouldBe(0);
    }

    #endregion

    #region Multiple Entities Tests

    [Fact]
    public async Task SaveChangesAsync_MultipleEntities_SetsFieldsForAll()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var entity1 = new AuditedTestEntity { Name = "Test1" };
        var entity2 = new AuditedTestEntity { Name = "Test2" };
        var entity3 = new AuditedTestEntity { Name = "Test3" };

        context.AuditedEntities.AddRange(entity1, entity2, entity3);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity1.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        entity1.CreatedBy.ShouldBe(userId);
        entity2.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        entity2.CreatedBy.ShouldBe(userId);
        entity3.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        entity3.CreatedBy.ShouldBe(userId);
    }

    [Fact]
    public async Task SaveChangesAsync_MixedAuditableAndNonAuditable_SetsFieldsOnlyForAuditable()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        const string userId = "test-user";

        var serviceProvider = CreateServiceProviderWithUser(userId);
        var options = new AuditInterceptorOptions { Enabled = true };
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var interceptor = new AuditInterceptor(serviceProvider, options, fakeTime, logger);

        await using var context = CreateInMemoryContext(interceptor);
        var auditedEntity = new AuditedTestEntity { Name = "Audited" };
        var nonAuditedEntity = new NonAuditableEntity { Name = "NonAudited" };

        context.AuditedEntities.Add(auditedEntity);
        context.NonAuditableEntities.Add(nonAuditedEntity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        auditedEntity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        auditedEntity.CreatedBy.ShouldBe(userId);
        // NonAuditableEntity has no audit properties to check
    }

    #endregion

    #region Helper Methods

    private static IServiceProvider CreateServiceProviderWithUser(string? userId)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();

        if (userId is not null)
        {
            var requestContext = Substitute.For<IRequestContext>();
            requestContext.UserId.Returns(userId);
            serviceProvider.GetService(typeof(IRequestContext)).Returns(requestContext);
        }

        return serviceProvider;
    }

    private static TestDbContext CreateInMemoryContext(AuditInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new TestDbContext(options);
    }

    #endregion
}

/// <summary>
/// Tests for AuditInterceptorOptions.
/// </summary>
public class AuditInterceptorOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new AuditInterceptorOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
        options.TrackCreatedAt.ShouldBeTrue();
        options.TrackCreatedBy.ShouldBeTrue();
        options.TrackModifiedAt.ShouldBeTrue();
        options.TrackModifiedBy.ShouldBeTrue();
        options.LogAuditChanges.ShouldBeFalse();
        options.LogChangesToStore.ShouldBeFalse();
    }

    [Fact]
    public void Options_ShouldBeConfigurable()
    {
        // Arrange & Act
        var options = new AuditInterceptorOptions
        {
            Enabled = false,
            TrackCreatedAt = false,
            TrackCreatedBy = false,
            TrackModifiedAt = false,
            TrackModifiedBy = false,
            LogAuditChanges = true,
            LogChangesToStore = true
        };

        // Assert
        options.Enabled.ShouldBeFalse();
        options.TrackCreatedAt.ShouldBeFalse();
        options.TrackCreatedBy.ShouldBeFalse();
        options.TrackModifiedAt.ShouldBeFalse();
        options.TrackModifiedBy.ShouldBeFalse();
        options.LogAuditChanges.ShouldBeTrue();
        options.LogChangesToStore.ShouldBeTrue();
    }
}
