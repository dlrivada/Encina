using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Deep guard tests for <see cref="AuditInterceptor"/> that exercise interceptor methods,
/// audit field population, audit log capture, and edge cases beyond constructor null checks.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class AuditInterceptorDeepGuardTests
{
    #region Audit Field Population via SaveChanges

    [Fact]
    public async Task SaveChangesAsync_AddedAuditableEntity_PopulatesCreationFields()
    {
        // Arrange - exercises: SavingChangesAsync -> PopulateAuditFields ->
        // iterates ChangeTracker entries -> EntityState.Added -> PopulateCreationFields ->
        // TrackCreatedAt (sets CreatedAtUtc), TrackCreatedBy (no user = skips)
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 10, 30, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            TrackCreatedBy = true,
            LogAuditChanges = false,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "test" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert - CreatedAtUtc populated by interceptor
        entity.CreatedAtUtc.ShouldBe(new DateTime(2026, 4, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedAuditableEntity_PopulatesModificationFields()
    {
        // Arrange - exercises: PopulateAuditFields -> EntityState.Modified ->
        // PopulateModificationFields -> TrackModifiedAt (sets ModifiedAtUtc)
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            TrackModifiedAt = true,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "original" };
        dbContext.Set<AuditableTestEntity>().Add(entity);
        await dbContext.SaveChangesAsync();

        // Advance time
        fakeTime.SetUtcNow(new DateTimeOffset(2026, 4, 15, 11, 0, 0, TimeSpan.Zero));

        // Modify
        entity.Name = "modified";
        dbContext.Entry(entity).State = EntityState.Modified;

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.ShouldBe(new DateTime(2026, 4, 15, 11, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SaveChanges_Sync_AddedAuditableEntity_PopulatesCreationFields()
    {
        // Arrange - exercises: sync SavingChanges -> PopulateAuditFields
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "sync-test" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act
        dbContext.SaveChanges();

        // Assert
        entity.CreatedAtUtc.ShouldBe(new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region Selective Tracking Disabled

    [Fact]
    public async Task SaveChangesAsync_TrackCreatedAtDisabled_DoesNotSetCreatedAtUtc()
    {
        // Arrange - exercises PopulateCreationFields where TrackCreatedAt = false
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = false,
            TrackCreatedBy = false,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "test" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert - should NOT be set since tracking is disabled
        entity.CreatedAtUtc.ShouldBe(default);
    }

    [Fact]
    public async Task SaveChangesAsync_TrackModifiedAtDisabled_DoesNotSetModifiedAtUtc()
    {
        // Arrange - exercises PopulateModificationFields where TrackModifiedAt = false
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            TrackModifiedAt = false,
            TrackModifiedBy = false,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "original" };
        dbContext.Set<AuditableTestEntity>().Add(entity);
        await dbContext.SaveChangesAsync();

        entity.Name = "modified";
        dbContext.Entry(entity).State = EntityState.Modified;

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        entity.ModifiedAtUtc.ShouldBeNull();
    }

    #endregion

    #region Enabled = false

    [Fact]
    public async Task SaveChangesAsync_OptionsDisabled_DoesNotPopulateAuditFields()
    {
        // Arrange - exercises SavingChangesAsync where Enabled=false,
        // skips PopulateAuditFields entirely
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = false,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "test" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.ShouldBe(default);
        entity.CreatedBy.ShouldBeNull();
    }

    #endregion

    #region Non-Auditable Entity

    [Fact]
    public async Task SaveChangesAsync_NonAuditableEntity_SkipsPopulation()
    {
        // Arrange - exercises PopulateAuditFields loop where entity doesn't implement
        // any audit interfaces, so PopulateCreationFields/PopulateModificationFields
        // checks all fail gracefully
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options);

        var entity = new NonAuditableTestEntity { Id = Guid.NewGuid(), Value = "test" };
        dbContext.Set<NonAuditableTestEntity>().Add(entity);

        // Act & Assert - should complete without error
        await Should.NotThrowAsync(async () => await dbContext.SaveChangesAsync());
    }

    #endregion

    #region Mixed Auditable and Non-Auditable Entities

    [Fact]
    public async Task SaveChangesAsync_MixedEntities_OnlyPopulatesAuditableOnes()
    {
        // Arrange - exercises PopulateAuditFields iterating over multiple entities
        // where only some implement IAuditableEntity interfaces
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime);

        var auditable = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "auditable" };
        var nonAuditable = new NonAuditableTestEntity { Id = Guid.NewGuid(), Value = "non-auditable" };
        dbContext.Set<AuditableTestEntity>().Add(auditable);
        dbContext.Set<NonAuditableTestEntity>().Add(nonAuditable);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert - only the auditable entity should have CreatedAtUtc set
        auditable.CreatedAtUtc.ShouldBe(new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region Audit Log Store Integration

    [Fact]
    public async Task SaveChangesAsync_LogChangesToStoreEnabled_WithAuditLogStore_PersistsEntries()
    {
        // Arrange - exercises: CaptureChangesForAuditLog (lines 297-344),
        // PersistAuditEntriesAsync (lines 453-492),
        // including: ChangeTracker iteration, action mapping, SerializeValues,
        // GetEntityId, creating AuditLogEntry, calling IAuditLogStore.LogAsync
        var auditLogStore = Substitute.For<IAuditLogStore>();
        auditLogStore.LogAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            LogChangesToStore = true,
            LogAuditChanges = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime, auditLogStore);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "audit-test" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act
        await dbContext.SaveChangesAsync();

        // Assert - audit log store should have been called
        await auditLogStore.Received(1).LogAsync(
            Arg.Is<AuditLogEntry>(e =>
                e.EntityType == nameof(AuditableTestEntity) &&
                e.Action == AuditAction.Created),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_LogChangesToStoreEnabled_NoAuditLogStore_SkipsLogging()
    {
        // Arrange - exercises: LogChangesToStore=true but _auditLogStore is null,
        // so the condition on line 111 fails and CaptureChangesForAuditLog is skipped
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            LogChangesToStore = true
        };
        // Do NOT pass auditLogStore - it stays null
        using var dbContext = CreateDbContextWithInterceptor(options, auditLogStore: null);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "test" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act & Assert - should not throw even without audit log store
        await Should.NotThrowAsync(async () => await dbContext.SaveChangesAsync());
    }

    [Fact]
    public void SaveChanges_Sync_LogChangesToStore_PersistsEntriesSync()
    {
        // Arrange - exercises the synchronous PersistAuditEntriesSync path (lines 407-447)
        var auditLogStore = Substitute.For<IAuditLogStore>();
        auditLogStore.LogAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero));
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            LogChangesToStore = true,
            LogAuditChanges = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options, fakeTime, auditLogStore);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "sync-audit" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act
        dbContext.SaveChanges();

        // Assert
        auditLogStore.Received(1).LogAsync(
            Arg.Is<AuditLogEntry>(e => e.Action == AuditAction.Created),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Audit Log Store Error Handling

    [Fact]
    public async Task SaveChangesAsync_AuditLogStoreThrows_DoesNotPropagateException()
    {
        // Arrange - exercises the catch block in PersistAuditEntriesAsync (lines 486-489)
        // which logs the error but does not rethrow
        var auditLogStore = Substitute.For<IAuditLogStore>();
        auditLogStore.LogAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Store unavailable")));

        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            LogChangesToStore = true
        };
        using var dbContext = CreateDbContextWithInterceptor(options, auditLogStore: auditLogStore);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "error-test" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act & Assert - should NOT throw, exception is caught and logged
        await Should.NotThrowAsync(async () => await dbContext.SaveChangesAsync());
    }

    [Fact]
    public void SaveChanges_Sync_AuditLogStoreThrows_DoesNotPropagateException()
    {
        // Arrange - exercises the catch block in PersistAuditEntriesSync (lines 439-442)
        var auditLogStore = Substitute.For<IAuditLogStore>();
        auditLogStore.LogAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Store unavailable")));

        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            TrackCreatedAt = true,
            LogChangesToStore = true
        };
        using var dbContext = CreateDbContextWithInterceptor(options, auditLogStore: auditLogStore);

        var entity = new AuditableTestEntity { Id = Guid.NewGuid(), Name = "sync-error" };
        dbContext.Set<AuditableTestEntity>().Add(entity);

        // Act & Assert - should NOT throw
        Should.NotThrow(() => dbContext.SaveChanges());
    }

    #endregion

    #region Constructor with AuditLogStore

    [Fact]
    public void Constructor_WithAuditLogStore_DoesNotThrow()
    {
        // Arrange - exercises constructor lines 89-98 (all fields including optional auditLogStore)
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new AuditInterceptorOptions();
        var timeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditInterceptor>>();
        var auditLogStore = Substitute.For<IAuditLogStore>();

        // Act & Assert
        var interceptor = new AuditInterceptor(serviceProvider, options, timeProvider, logger, auditLogStore);
        interceptor.ShouldNotBeNull();
    }

    #endregion

    #region No Tracked Entities

    [Fact]
    public async Task SaveChangesAsync_NoTrackedEntities_SkipsPopulation()
    {
        // Arrange - exercises PopulateAuditFields with empty ChangeTracker
        var options = new AuditInterceptorOptions
        {
            Enabled = true,
            LogChangesToStore = false
        };
        using var dbContext = CreateDbContextWithInterceptor(options);

        // Act & Assert - no entities tracked, should work fine
        await Should.NotThrowAsync(async () => await dbContext.SaveChangesAsync());
    }

    #endregion

    #region Test Infrastructure

    private static TestAuditDbContext CreateDbContextWithInterceptor(
        AuditInterceptorOptions? options = null,
        TimeProvider? timeProvider = null,
        IAuditLogStore? auditLogStore = null)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<AuditInterceptor>>();

        var interceptor = new AuditInterceptor(
            serviceProvider,
            options ?? new AuditInterceptorOptions(),
            timeProvider ?? TimeProvider.System,
            logger,
            auditLogStore);

        var dbOptions = new DbContextOptionsBuilder<TestAuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new TestAuditDbContext(dbOptions);
    }

    internal sealed class TestAuditDbContext : DbContext
    {
        public TestAuditDbContext(DbContextOptions<TestAuditDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditableTestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<NonAuditableTestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    internal sealed class AuditableTestEntity : ICreatedAtUtc, ICreatedBy, IModifiedAtUtc, IModifiedBy
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
    }

    internal sealed class NonAuditableTestEntity
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    #endregion
}
