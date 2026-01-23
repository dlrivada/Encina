using System.Linq.Expressions;
using Encina.MongoDB.Tenancy;
using Encina.Tenancy;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.MongoDB.Tenancy;

/// <summary>
/// Guard clause tests for <see cref="TenantAwareFunctionalRepositoryMongoDB{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "MongoDB")]
public sealed class TenantAwareFunctionalRepositoryMongoDBGuardTests
{
    private static readonly Expression<Func<TenantGuardTestOrder, Guid>> IdSelector = o => o.Id;

    [Fact]
    public void Constructor_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IMongoCollection<TenantGuardTestOrder> collection = null!;
        var mapping = CreateMockMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new MongoDbTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryMongoDB<TenantGuardTestOrder, Guid>(
                collection, mapping, tenantProvider, options, IdSelector));
        ex.ParamName.ShouldBe("collection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<TenantGuardTestOrder>>();
        ITenantEntityMapping<TenantGuardTestOrder, Guid> mapping = null!;
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new MongoDbTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryMongoDB<TenantGuardTestOrder, Guid>(
                collection, mapping, tenantProvider, options, IdSelector));
        ex.ParamName.ShouldBe("mapping");
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<TenantGuardTestOrder>>();
        var mapping = CreateMockMapping();
        ITenantProvider tenantProvider = null!;
        var options = new MongoDbTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryMongoDB<TenantGuardTestOrder, Guid>(
                collection, mapping, tenantProvider, options, IdSelector));
        ex.ParamName.ShouldBe("tenantProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<TenantGuardTestOrder>>();
        var mapping = CreateMockMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        MongoDbTenancyOptions options = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryMongoDB<TenantGuardTestOrder, Guid>(
                collection, mapping, tenantProvider, options, IdSelector));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullIdSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<TenantGuardTestOrder>>();
        var mapping = CreateMockMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new MongoDbTenancyOptions();
        Expression<Func<TenantGuardTestOrder, Guid>> idSelector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryMongoDB<TenantGuardTestOrder, Guid>(
                collection, mapping, tenantProvider, options, idSelector));
        ex.ParamName.ShouldBe("idSelector");
    }

    private static ITenantEntityMapping<TenantGuardTestOrder, Guid> CreateMockMapping()
    {
        var mapping = Substitute.For<ITenantEntityMapping<TenantGuardTestOrder, Guid>>();
        mapping.CollectionName.Returns("orders");
        mapping.IdFieldName.Returns("_id");
        mapping.IsTenantEntity.Returns(true);
        mapping.TenantFieldName.Returns("TenantId");
        return mapping;
    }
}
