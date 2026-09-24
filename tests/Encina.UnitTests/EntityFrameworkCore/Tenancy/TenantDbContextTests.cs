using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Configuration;
using Encina.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantDbContext"/>.
/// </summary>
public sealed class TenantDbContextTests
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IOptions<EfCoreTenancyOptions> _efCoreOptions;
    private readonly IOptions<TenancyOptions> _coreOptions;

    public TenantDbContextTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _efCoreOptions = Options.Create(new EfCoreTenancyOptions());
        _coreOptions = Options.Create(new TenancyOptions());
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestTenantDbContext>();
        optionsBuilder.UseInMemoryDatabase("TestDb");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestTenantDbContext(
                optionsBuilder.Options,
                null!,
                _efCoreOptions,
                _coreOptions));
    }

    [Fact]
    public void Constructor_NullEfCoreOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestTenantDbContext>();
        optionsBuilder.UseInMemoryDatabase("TestDb");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestTenantDbContext(
                optionsBuilder.Options,
                _tenantProvider,
                null!,
                _coreOptions));
    }

    [Fact]
    public void Constructor_NullCoreOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestTenantDbContext>();
        optionsBuilder.UseInMemoryDatabase("TestDb");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestTenantDbContext(
                optionsBuilder.Options,
                _tenantProvider,
                _efCoreOptions,
                null!));
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestTenantDbContext>();
        optionsBuilder.UseInMemoryDatabase("TestDb");

        // Act & Assert
        Should.NotThrow(() =>
            new TestTenantDbContext(
                optionsBuilder.Options,
                _tenantProvider,
                _efCoreOptions,
                _coreOptions));
    }

    #endregion

    #region SaveChanges Tenant Assignment Tests

    [Fact]
    public async Task SaveChangesAsync_NewEntity_AutoAssignsTenantId()
    {
        // Arrange
        var tenantId = "tenant-123";
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);

        using var context = CreateTestContext();
        var entity = new TenantTestEntity { Name = "Test Entity" };
        context.TenantEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_NewEntity_AutoAssignDisabled_DoesNotAssignTenantId()
    {
        // Arrange
        var tenantId = "tenant-123";
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);
        var options = Options.Create(new EfCoreTenancyOptions { AutoAssignTenantId = false });

        using var context = CreateTestContext(options);
        var entity = new TenantTestEntity { Name = "Test Entity", TenantId = "original-tenant" };
        context.TenantEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert - TenantId remains unchanged
        entity.TenantId.ShouldBe("original-tenant");
    }

    [Fact]
    public async Task SaveChangesAsync_NoTenantContext_RequireTenantTrue_ThrowsException()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var coreOptions = Options.Create(new TenancyOptions { RequireTenant = true });
        var efCoreOptions = Options.Create(new EfCoreTenancyOptions { ThrowOnMissingTenantContext = true });

        using var context = CreateTestContext(efCoreOptions, coreOptions);
        var entity = new TenantTestEntity { Name = "Test Entity" };
        context.TenantEntities.Add(entity);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await context.SaveChangesAsync());
        exception.Message.ShouldContain("without tenant context");
    }

    [Fact]
    public async Task SaveChangesAsync_NoTenantContext_RequireTenantFalse_DoesNotThrow()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var coreOptions = Options.Create(new TenancyOptions { RequireTenant = false });

        using var context = CreateTestContext(_efCoreOptions, coreOptions);
        var entity = new TenantTestEntity { Name = "Test Entity", TenantId = "pre-set-tenant" };
        context.TenantEntities.Add(entity);

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await context.SaveChangesAsync());
    }

    #endregion

    #region SaveChanges Tenant Validation Tests

    [Fact]
    public async Task SaveChangesAsync_ModifyEntity_DifferentTenant_ThrowsException()
    {
        // Arrange
        var currentTenantId = "tenant-current";
        _tenantProvider.GetCurrentTenantId().Returns(currentTenantId);

        // Create entity with different tenant using no-auto-assign option
        var noAssignOptions = Options.Create(new EfCoreTenancyOptions { AutoAssignTenantId = false, ValidateTenantOnSave = false });
        using var setupContext = CreateTestContext(noAssignOptions);
        var entity = new TenantTestEntity { TenantId = "tenant-other", Name = "Test Entity" };
        setupContext.TenantEntities.Add(entity);
        await setupContext.SaveChangesAsync();
        var entityId = entity.Id;

        // Re-create context with validation enabled to simulate modification from different tenant
        var validatingOptions = Options.Create(new EfCoreTenancyOptions { ValidateTenantOnSave = true });
        using var validatingContext = CreateTestContext(validatingOptions);
        var attachedEntity = new TenantTestEntity { Id = entityId, TenantId = "tenant-other", Name = "Modified Name" };
        validatingContext.Attach(attachedEntity);
        validatingContext.Entry(attachedEntity).State = EntityState.Modified;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await validatingContext.SaveChangesAsync());
        exception.Message.ShouldContain("Tenant mismatch");
    }

    [Fact]
    public async Task SaveChangesAsync_ModifyEntity_SameTenant_Succeeds()
    {
        // Arrange
        var currentTenantId = "tenant-123";
        _tenantProvider.GetCurrentTenantId().Returns(currentTenantId);

        using var context = CreateTestContext();
        var entity = new TenantTestEntity { Name = "Test Entity" };
        context.TenantEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        entity.Name = "Modified Name";
        await context.SaveChangesAsync();

        // Assert - No exception thrown
        entity.Name.ShouldBe("Modified Name");
        entity.TenantId.ShouldBe(currentTenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_ValidateTenantDisabled_AllowsCrossTenantAccess()
    {
        // Arrange
        var currentTenantId = "tenant-current";
        _tenantProvider.GetCurrentTenantId().Returns(currentTenantId);
        var options = Options.Create(new EfCoreTenancyOptions { AutoAssignTenantId = false, ValidateTenantOnSave = false });

        using var context = CreateTestContext(options);

        // Create entity belonging to different tenant
        var entity = new TenantTestEntity { TenantId = "tenant-other", Name = "Test Entity" };
        context.TenantEntities.Add(entity);
        await context.SaveChangesAsync();

        // Modify entity (should succeed since validation is disabled)
        entity.Name = "Modified Name";

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(async () =>
            await context.SaveChangesAsync());
    }

    #endregion

    #region Synchronous SaveChanges Tests

    [Fact]
    public void SaveChanges_NewEntity_AutoAssignsTenantId()
    {
        // Arrange
        var tenantId = "tenant-123";
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);

        using var context = CreateTestContext();
        var entity = new TenantTestEntity { Name = "Test Entity" };
        context.TenantEntities.Add(entity);

        // Act
        context.SaveChanges();

        // Assert
        entity.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public void SaveChanges_AcceptAllChangesOnSuccess_AutoAssignsTenantId()
    {
        // Arrange
        var tenantId = "tenant-123";
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);

        using var context = CreateTestContext();
        var entity = new TenantTestEntity { Name = "Test Entity" };
        context.TenantEntities.Add(entity);

        // Act
        context.SaveChanges(acceptAllChangesOnSuccess: true);

        // Assert
        entity.TenantId.ShouldBe(tenantId);
    }

    #endregion

    #region Named Query Filter Coexistence Tests (#1268)

    [Fact]
    public void OnModelCreating_TenantFilterAppliedBeforeSoftDelete_BothNamedFiltersCoexist()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TenantThenSoftDeleteDbContext>();
        optionsBuilder.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");

        // Act
        using var context = new TenantThenSoftDeleteDbContext(
            optionsBuilder.Options,
            _tenantProvider,
            _efCoreOptions,
            _coreOptions);
        var entityType = context.Model.FindEntityType(typeof(TenantSoftDeleteTestEntity))!;

        // Assert - both named filters are present, neither overwrote the other
        entityType.FindDeclaredQueryFilter(TenantDbContext.TenantQueryFilterKey).ShouldNotBeNull();
        entityType.FindDeclaredQueryFilter(EntityConfigurationExtensions.SoftDeleteQueryFilterKey).ShouldNotBeNull();
    }

    [Fact]
    public void OnModelCreating_SoftDeleteFilterAppliedBeforeTenant_BothNamedFiltersCoexist()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<SoftDeleteThenTenantDbContext>();
        optionsBuilder.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");

        // Act
        using var context = new SoftDeleteThenTenantDbContext(
            optionsBuilder.Options,
            _tenantProvider,
            _efCoreOptions,
            _coreOptions);
        var entityType = context.Model.FindEntityType(typeof(TenantSoftDeleteTestEntity))!;

        // Assert - order of the two Apply* calls does not matter
        entityType.FindDeclaredQueryFilter(TenantDbContext.TenantQueryFilterKey).ShouldNotBeNull();
        entityType.FindDeclaredQueryFilter(EntityConfigurationExtensions.SoftDeleteQueryFilterKey).ShouldNotBeNull();
    }

    #endregion

    #region Helper Methods

    private TestTenantDbContext CreateTestContext(
        IOptions<EfCoreTenancyOptions>? efCoreOptions = null,
        IOptions<TenancyOptions>? coreOptions = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestTenantDbContext>();
        optionsBuilder.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");

        return new TestTenantDbContext(
            optionsBuilder.Options,
            _tenantProvider,
            efCoreOptions ?? _efCoreOptions,
            coreOptions ?? _coreOptions);
    }

    #endregion

    #region Test Classes

    /// <summary>
    /// Concrete implementation of TenantDbContext for testing.
    /// </summary>
    private sealed class TestTenantDbContext : TenantDbContext
    {
        public TestTenantDbContext(
            DbContextOptions options,
            ITenantProvider tenantProvider,
            IOptions<EfCoreTenancyOptions> tenancyOptions,
            IOptions<TenancyOptions> coreOptions,
            ITenantSchemaConfigurator? schemaConfigurator = null)
            : base(options, tenantProvider, tenancyOptions, coreOptions, schemaConfigurator)
        {
        }

        public DbSet<TenantTestEntity> TenantEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TenantTestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });
        }
    }

    /// <summary>
    /// Test entity implementing ITenantEntity.
    /// </summary>
    private sealed class TenantTestEntity : ITenantEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test entity implementing both <see cref="ITenantEntity"/> and <see cref="ISoftDeletable"/>,
    /// used to verify that the tenant and soft-delete query filters coexist (#1268).
    /// </summary>
    private sealed class TenantSoftDeleteTestEntity : ITenantEntity, ISoftDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }

    /// <summary>
    /// Applies the tenant filter first (via <c>base.OnModelCreating</c>) and the soft-delete
    /// filter afterward, mirroring the order described in the #1268 repro steps.
    /// </summary>
    private sealed class TenantThenSoftDeleteDbContext : TenantDbContext
    {
        public TenantThenSoftDeleteDbContext(
            DbContextOptions options,
            ITenantProvider tenantProvider,
            IOptions<EfCoreTenancyOptions> tenancyOptions,
            IOptions<TenancyOptions> coreOptions)
            : base(options, tenantProvider, tenancyOptions, coreOptions)
        {
        }

        public DbSet<TenantSoftDeleteTestEntity> Entities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Applies the "Encina.Tenancy" named filter.

            modelBuilder.Entity<TenantSoftDeleteTestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.ApplySoftDeleteQueryFilters(); // Applies the "Encina.SoftDelete" named filter.
        }
    }

    /// <summary>
    /// Applies the soft-delete filter first and the tenant filter afterward (via
    /// <c>base.OnModelCreating</c>), the reverse order of <see cref="TenantThenSoftDeleteDbContext"/>.
    /// </summary>
    private sealed class SoftDeleteThenTenantDbContext : TenantDbContext
    {
        public SoftDeleteThenTenantDbContext(
            DbContextOptions options,
            ITenantProvider tenantProvider,
            IOptions<EfCoreTenancyOptions> tenancyOptions,
            IOptions<TenancyOptions> coreOptions)
            : base(options, tenantProvider, tenancyOptions, coreOptions)
        {
        }

        public DbSet<TenantSoftDeleteTestEntity> Entities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantSoftDeleteTestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.ApplySoftDeleteQueryFilters(); // Applies the "Encina.SoftDelete" named filter first.

            base.OnModelCreating(modelBuilder); // Applies the "Encina.Tenancy" named filter afterward.
        }
    }

    #endregion
}
