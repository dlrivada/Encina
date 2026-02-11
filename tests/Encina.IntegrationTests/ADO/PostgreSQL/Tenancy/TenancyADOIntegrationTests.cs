using System.Data;
using Encina.ADO.PostgreSQL.Tenancy;
using Encina.DomainModeling;
using Encina.Tenancy;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Npgsql;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.ADO.PostgreSQL.Tenancy;

/// <summary>
/// Integration tests for multi-tenancy support in ADO.NET PostgreSQL provider.
/// Tests automatic tenant filtering, tenant ID assignment, and cross-tenant isolation.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public class TenancyADOIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private IDbConnection _connection = null!;
    private TenantAwareFunctionalRepositoryADO<TenantTestEntity, Guid> _repository = null!;
    private ITenantEntityMapping<TenantTestEntity, Guid> _mapping = null!;
    private TestTenantProvider _tenantProvider = null!;
    private ADOTenancyOptions _tenancyOptions = null!;

    private const string Tenant1 = "tenant-001";
    private const string Tenant2 = "tenant-002";

    public TenancyADOIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        using var schemaConnection = _fixture.CreateConnection() as NpgsqlConnection;
        if (schemaConnection != null)
        {
            await TenancySchema.CreateTenantTestEntitiesSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _tenantProvider = new TestTenantProvider(Tenant1);
        _tenancyOptions = new ADOTenancyOptions
        {
            AutoFilterTenantQueries = true,
            AutoAssignTenantId = true,
            ValidateTenantOnModify = true,
            ThrowOnMissingTenantContext = true
        };

        _mapping = new TenantEntityMappingBuilder<TenantTestEntity, Guid>()
            .ToTable("tenanttestentities")
            .HasId(e => e.Id, "id")  // PostgreSQL is case-sensitive with quoted identifiers
            .HasTenantId(e => e.TenantId, "tenantid")
            .MapProperty(e => e.Name, "name")
            .MapProperty(e => e.Description, "description")
            .MapProperty(e => e.Amount, "amount")
            .MapProperty(e => e.IsActive, "isactive")
            .MapProperty(e => e.CreatedAtUtc, "createdatutc")
            .MapProperty(e => e.UpdatedAtUtc, "updatedatutc")
            .Build();

        _repository = new TenantAwareFunctionalRepositoryADO<TenantTestEntity, Guid>(
            _connection, _mapping, _tenantProvider, _tenancyOptions);
    }

    public async ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is NpgsqlConnection npgsqlConnection)
        {
            await TenancySchema.ClearTenancyDataAsync(npgsqlConnection);
        }
    }

    private void SwitchTenant(string tenantId)
    {
        _tenantProvider.SetCurrentTenant(tenantId);
        _repository = new TenantAwareFunctionalRepositoryADO<TenantTestEntity, Guid>(
            _connection, _mapping, _tenantProvider, _tenancyOptions);
    }

    #region Automatic Tenant Filter Tests

    [Fact]
    public async Task ListAsync_OnlyReturnsCurrentTenantData()
    {

        // Arrange
        await ClearDataAsync();

        _tenantProvider.SetCurrentTenant(Tenant1);
        _repository = new TenantAwareFunctionalRepositoryADO<TenantTestEntity, Guid>(
            _connection, _mapping, _tenantProvider, _tenancyOptions);
        await _repository.AddAsync(CreateEntity("Tenant1-Entity1"));
        await _repository.AddAsync(CreateEntity("Tenant1-Entity2"));

        SwitchTenant(Tenant2);
        await _repository.AddAsync(CreateEntity("Tenant2-Entity1"));

        // Act
        SwitchTenant(Tenant1);
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(2);
            list.ShouldAllBe(e => e.TenantId == Tenant1);
        });
    }

    [Fact]
    public async Task GetByIdAsync_OnlyReturnsCurrentTenantEntity()
    {

        // Arrange
        await ClearDataAsync();

        SwitchTenant(Tenant1);
        var tenant1Entity = CreateEntity("Tenant1-Entity");
        await _repository.AddAsync(tenant1Entity);

        // Act
        SwitchTenant(Tenant2);
        var result = await _repository.GetByIdAsync(tenant1Entity.Id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task ListAsync_WithSpecification_AppliesTenantFilterWithSpec()
    {

        // Arrange
        await ClearDataAsync();

        SwitchTenant(Tenant1);
        await _repository.AddAsync(CreateEntity("Active1", isActive: true));
        await _repository.AddAsync(CreateEntity("Inactive1", isActive: false));

        SwitchTenant(Tenant2);
        await _repository.AddAsync(CreateEntity("Active2", isActive: true));

        // Act
        SwitchTenant(Tenant1);
        var spec = new IsActiveTenantSpec();
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Active1");
            list[0].TenantId.ShouldBe(Tenant1);
        });
    }

    #endregion

    #region Automatic Tenant ID Assignment Tests

    [Fact]
    public async Task AddAsync_AutomaticallyAssignsTenantId()
    {

        // Arrange
        await ClearDataAsync();
        SwitchTenant(Tenant1);
        var entity = CreateEntity("New Entity");
        entity.TenantId = string.Empty;

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.TenantId.ShouldBe(Tenant1));

        var retrieved = await _repository.GetByIdAsync(entity.Id);
        retrieved.IsRight.ShouldBeTrue();
        retrieved.IfRight(e => e.TenantId.ShouldBe(Tenant1));
    }

    [Fact]
    public async Task AddRangeAsync_AssignsTenantIdToAllEntities()
    {

        // Arrange
        await ClearDataAsync();
        SwitchTenant(Tenant1);

        var entities = new[]
        {
            CreateEntity("Entity1"),
            CreateEntity("Entity2"),
            CreateEntity("Entity3")
        };

        foreach (var e in entities)
        {
            e.TenantId = string.Empty;
        }

        // Act
        var result = await _repository.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        var listResult = await _repository.ListAsync();
        listResult.IsRight.ShouldBeTrue();
        listResult.IfRight(list =>
        {
            list.Count.ShouldBe(3);
            list.ShouldAllBe(e => e.TenantId == Tenant1);
        });
    }

    #endregion

    #region Cross-Tenant Isolation Tests

    [Fact]
    public async Task UpdateAsync_CanOnlyUpdateOwnTenantEntities()
    {

        // Arrange
        await ClearDataAsync();

        SwitchTenant(Tenant1);
        var entity = CreateEntity("Original");
        await _repository.AddAsync(entity);

        SwitchTenant(Tenant2);
        entity.Name = "Modified";

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();

        SwitchTenant(Tenant1);
        var retrieved = await _repository.GetByIdAsync(entity.Id);
        retrieved.IsRight.ShouldBeTrue();
        retrieved.IfRight(e => e.Name.ShouldBe("Original"));
    }

    [Fact]
    public async Task DeleteAsync_CanOnlyDeleteOwnTenantEntities()
    {

        // Arrange
        await ClearDataAsync();

        SwitchTenant(Tenant1);
        var entity = CreateEntity("ToDelete");
        await _repository.AddAsync(entity);

        SwitchTenant(Tenant2);

        // Act
        var result = await _repository.DeleteAsync(entity.Id);

        // Assert
        result.IsLeft.ShouldBeTrue();

        SwitchTenant(Tenant1);
        var retrieved = await _repository.GetByIdAsync(entity.Id);
        retrieved.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CountAsync_OnlyCountsCurrentTenantEntities()
    {

        // Arrange
        await ClearDataAsync();

        SwitchTenant(Tenant1);
        await _repository.AddAsync(CreateEntity("T1-E1"));
        await _repository.AddAsync(CreateEntity("T1-E2"));

        SwitchTenant(Tenant2);
        await _repository.AddAsync(CreateEntity("T2-E1"));
        await _repository.AddAsync(CreateEntity("T2-E2"));
        await _repository.AddAsync(CreateEntity("T2-E3"));

        // Act
        SwitchTenant(Tenant1);
        var result1 = await _repository.CountAsync();

        SwitchTenant(Tenant2);
        var result2 = await _repository.CountAsync();

        // Assert
        result1.IsRight.ShouldBeTrue();
        result1.IfRight(count => count.ShouldBe(2));

        result2.IsRight.ShouldBeTrue();
        result2.IfRight(count => count.ShouldBe(3));
    }

    [Fact]
    public async Task AnyAsync_OnlyChecksCurrentTenantEntities()
    {

        // Arrange
        await ClearDataAsync();

        SwitchTenant(Tenant1);
        await _repository.AddAsync(CreateEntity("T1-E1"));

        // Act
        SwitchTenant(Tenant1);
        var resultTenant1 = await _repository.AnyAsync();

        SwitchTenant(Tenant2);
        var resultTenant2 = await _repository.AnyAsync();

        // Assert
        resultTenant1.IsRight.ShouldBeTrue();
        resultTenant1.IfRight(any => any.ShouldBeTrue());

        resultTenant2.IsRight.ShouldBeTrue();
        resultTenant2.IfRight(any => any.ShouldBeFalse());
    }

    #endregion

    #region Tenant Context Switching Tests

    [Fact]
    public async Task TenantSwitch_ChangesVisibleData()
    {

        // Arrange
        await ClearDataAsync();

        SwitchTenant(Tenant1);
        await _repository.AddAsync(CreateEntity("Tenant1-Data"));

        SwitchTenant(Tenant2);
        await _repository.AddAsync(CreateEntity("Tenant2-Data"));

        // Act & Assert
        SwitchTenant(Tenant1);
        var result1 = await _repository.ListAsync();
        result1.IsRight.ShouldBeTrue();
        result1.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Tenant1-Data");
        });

        SwitchTenant(Tenant2);
        var result2 = await _repository.ListAsync();
        result2.IsRight.ShouldBeTrue();
        result2.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Tenant2-Data");
        });
    }

    #endregion

    #region Helper Methods

    private static TenantTestEntity CreateEntity(
        string name = "Test Entity",
        bool isActive = true,
        decimal amount = 100m)
    {
        return new TenantTestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = string.Empty,
            Name = name,
            Description = null,
            Amount = amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }

    #endregion
}

#region Test Tenant Provider

internal sealed class TestTenantProvider : ITenantProvider
{
    private string? _currentTenantId;

    public TestTenantProvider(string? initialTenantId = null)
    {
        _currentTenantId = initialTenantId;
    }

    public void SetCurrentTenant(string? tenantId)
    {
        _currentTenantId = tenantId;
    }

    public string? GetCurrentTenantId() => _currentTenantId;

    public ValueTask<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTenantId is null)
            return ValueTask.FromResult<TenantInfo?>(null);

        return ValueTask.FromResult<TenantInfo?>(new TenantInfo(
            TenantId: _currentTenantId,
            Name: $"Test Tenant {_currentTenantId}",
            Strategy: TenantIsolationStrategy.SharedSchema));
    }
}

#endregion

#region Test Specifications

internal sealed class IsActiveTenantSpec : Specification<TenantTestEntity>
{
    public override System.Linq.Expressions.Expression<Func<TenantTestEntity, bool>> ToExpression()
        => e => e.IsActive;
}

#endregion
