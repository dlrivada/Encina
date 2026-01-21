using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.MongoDB.Tenancy;
using Encina.Tenancy;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantAwareSpecificationFilterBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantAwareSpecificationFilterBuilderTests
{
    private readonly ITenantProvider _tenantProvider;
    private readonly MongoDbTenancyOptions _options;
    private readonly ITenantEntityMapping<MongoTenantTestOrder, object> _tenantMapping;
    private readonly ITenantEntityMapping<MongoTenantTestOrder, object> _nonTenantMapping;

    public TenantAwareSpecificationFilterBuilderTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _options = new MongoDbTenancyOptions();

        // Create tenant entity mapping
        var tenantMappingBuilder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapField(o => o.CustomerId)
            .MapField(o => o.Total)
            .MapField(o => o.IsActive);

        var tenantMapping = tenantMappingBuilder.Build();
        _tenantMapping = new GenericTenantMappingAdapterForTests<MongoTenantTestOrder, Guid>(tenantMapping);

        // Create non-tenant entity mapping
        var nonTenantMappingBuilder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId)
            .MapField(o => o.Total);

        var nonTenantMapping = nonTenantMappingBuilder.Build();
        _nonTenantMapping = new GenericTenantMappingAdapterForTests<MongoTenantTestOrder, Guid>(nonTenantMapping);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(null!, _tenantProvider, _options));
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, null!));
    }

    #endregion

    #region BuildFilter Tests

    [Fact]
    public void BuildFilter_WithTenantContext_ReturnsNonNullFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, _options);
        var spec = new MongoActiveOrdersSpec();

        // Act
        var filter = builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
        filter.ShouldNotBe(Builders<MongoTenantTestOrder>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithoutTenantContext_ThrowsException_WhenConfigured()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var options = new MongoDbTenancyOptions { ThrowOnMissingTenantContext = true };
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new MongoActiveOrdersSpec();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.BuildFilter(spec))
            .Message.ShouldContain("without tenant context");
    }

    [Fact]
    public void BuildFilter_WithoutTenantContext_NoTenantFilter_WhenNotThrowConfigured()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var options = new MongoDbTenancyOptions { ThrowOnMissingTenantContext = false };
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new MongoActiveOrdersSpec();

        // Act
        var filter = builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_NonTenantEntity_NoTenantFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_nonTenantMapping, _tenantProvider, _options);
        var spec = new MongoActiveOrdersSpec();

        // Act
        var filter = builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_AutoFilterDisabled_NoTenantFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var options = new MongoDbTenancyOptions { AutoFilterTenantQueries = false };
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new MongoActiveOrdersSpec();

        // Act
        var filter = builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    #endregion

    #region BuildTenantFilter Tests

    [Fact]
    public void BuildTenantFilter_WithTenantContext_ReturnsNonEmptyFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-abc");
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, _options);

        // Act
        var filter = builder.BuildTenantFilter();

        // Assert
        filter.ShouldNotBeNull();
        filter.ShouldNotBe(Builders<MongoTenantTestOrder>.Filter.Empty);
    }

    [Fact]
    public void BuildTenantFilter_WithoutTenantContext_ReturnsEmptyFilter_WhenNotThrowConfigured()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var options = new MongoDbTenancyOptions { ThrowOnMissingTenantContext = false };
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, options);

        // Act
        var filter = builder.BuildTenantFilter();

        // Assert
        filter.ShouldBe(Builders<MongoTenantTestOrder>.Filter.Empty);
    }

    [Fact]
    public void BuildTenantFilter_NonTenantEntity_ReturnsEmptyFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_nonTenantMapping, _tenantProvider, _options);

        // Act
        var filter = builder.BuildTenantFilter();

        // Assert
        filter.ShouldBe(Builders<MongoTenantTestOrder>.Filter.Empty);
    }

    #endregion

    #region BuildFilterWithTenantId Tests

    [Fact]
    public void BuildFilterWithTenantId_WithTenantContext_ReturnsTenantId()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-xyz");
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, _options);

        // Act
        var (filter, tenantId) = builder.BuildFilterWithTenantId(null);

        // Assert
        tenantId.ShouldBe("tenant-xyz");
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilterWithTenantId_WithSpecification_ReturnsCombinedFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-xyz");
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, _options);
        var spec = new MongoActiveOrdersSpec();

        // Act
        var (filter, tenantId) = builder.BuildFilterWithTenantId(spec);

        // Assert
        tenantId.ShouldBe("tenant-xyz");
        filter.ShouldNotBeNull();
        filter.ShouldNotBe(Builders<MongoTenantTestOrder>.Filter.Empty);
    }

    #endregion

    #region GetCurrentTenantId Tests

    [Fact]
    public void GetCurrentTenantId_WithTenantContext_ReturnsTenantId()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-abc");
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, _options);

        // Act
        var tenantId = builder.GetCurrentTenantId();

        // Assert
        tenantId.ShouldBe("tenant-abc");
    }

    [Fact]
    public void GetCurrentTenantId_AutoFilterDisabled_ReturnsNull()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-abc");
        var options = new MongoDbTenancyOptions { AutoFilterTenantQueries = false };
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_tenantMapping, _tenantProvider, options);

        // Act
        var tenantId = builder.GetCurrentTenantId();

        // Assert
        tenantId.ShouldBeNull();
    }

    [Fact]
    public void GetCurrentTenantId_NonTenantEntity_ReturnsNull()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-abc");
        var builder = new TenantAwareSpecificationFilterBuilder<MongoTenantTestOrder>(_nonTenantMapping, _tenantProvider, _options);

        // Act
        var tenantId = builder.GetCurrentTenantId();

        // Assert
        tenantId.ShouldBeNull();
    }

    #endregion
}

/// <summary>
/// Internal adapter for tests - allows converting typed mapping to object-based.
/// </summary>
internal sealed class GenericTenantMappingAdapterForTests<TEntity, TId> : ITenantEntityMapping<TEntity, object>
    where TEntity : class
    where TId : notnull
{
    private readonly ITenantEntityMapping<TEntity, TId> _innerMapping;

    public GenericTenantMappingAdapterForTests(ITenantEntityMapping<TEntity, TId> innerMapping)
    {
        _innerMapping = innerMapping;
    }

    public string CollectionName => _innerMapping.CollectionName;
    public string IdFieldName => _innerMapping.IdFieldName;
    public IReadOnlyDictionary<string, string> FieldMappings => _innerMapping.FieldMappings;
    public bool IsTenantEntity => _innerMapping.IsTenantEntity;
    public string? TenantFieldName => _innerMapping.TenantFieldName;
    public string? TenantPropertyName => _innerMapping.TenantPropertyName;

    public object GetId(TEntity entity) => _innerMapping.GetId(entity)!;
    public string? GetTenantId(TEntity entity) => _innerMapping.GetTenantId(entity);
    public void SetTenantId(TEntity entity, string tenantId) => _innerMapping.SetTenantId(entity, tenantId);
}

#region Test Specifications

/// <summary>
/// Specification that filters for active MongoDB orders.
/// </summary>
internal sealed class MongoActiveOrdersSpec : Specification<MongoTenantTestOrder>
{
    public override Expression<Func<MongoTenantTestOrder, bool>> ToExpression()
        => o => o.IsActive;
}

/// <summary>
/// Specification that filters for orders with minimum total.
/// </summary>
internal sealed class MongoMinTotalSpec : Specification<MongoTenantTestOrder>
{
    private readonly decimal _minTotal;

    public MongoMinTotalSpec(decimal minTotal) => _minTotal = minTotal;

    public override Expression<Func<MongoTenantTestOrder, bool>> ToExpression()
        => o => o.Total > _minTotal;
}

#endregion
