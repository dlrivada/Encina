using System.Data;
using Encina.Dapper.Oracle.Tenancy;
using Encina.DomainModeling;
using Encina.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.Oracle.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenancyServiceCollectionExtensions"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenancyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaDapperWithTenancy_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add required dependencies
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Options.Create(new TenancyOptions
        {
            DefaultConnectionString = "Data Source=localhost;User Id=test;Password=test"
        }));

        // Act
        services.AddEncinaDapperWithTenancy(config =>
        {
            // No patterns enabled for this test
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var tenancyOptions = provider.GetService<IOptions<DapperTenancyOptions>>();
        tenancyOptions.ShouldNotBeNull();
        tenancyOptions.Value.AutoFilterTenantQueries.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaDapperWithTenancy_WithCustomOptions_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Options.Create(new TenancyOptions
        {
            DefaultConnectionString = "Data Source=localhost;User Id=test;Password=test"
        }));

        // Act
        services.AddEncinaDapperWithTenancy(
            config => { },
            tenancy =>
            {
                tenancy.AutoFilterTenantQueries = false;
                tenancy.TenantColumnName = "OrganizationId";
            });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DapperTenancyOptions>>().Value;
        options.AutoFilterTenantQueries.ShouldBeFalse();
        options.TenantColumnName.ShouldBe("OrganizationId");
    }

    [Fact]
    public void AddEncinaDapperWithTenancy_RegistersTenantConnectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Options.Create(new TenancyOptions
        {
            DefaultConnectionString = "Data Source=localhost;User Id=test;Password=test"
        }));

        // Act
        services.AddEncinaDapperWithTenancy(config => { });

        // Assert - Check that ITenantConnectionFactory is registered
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITenantConnectionFactory));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddTenantAwareRepository_RegistersRepositoryWithCorrectMapping()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = Substitute.For<IDbConnection>();

        services.AddSingleton(mockConnection);
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Options.Create(new DapperTenancyOptions()));

        // Act
        services.AddTenantAwareRepository<DapperOracleTenantTestOrder, Guid>(mapping =>
            mapping.ToTable("Orders")
                   .HasId(o => o.Id)
                   .HasTenantId(o => o.TenantId)
                   .MapProperty(o => o.CustomerId)
                   .MapProperty(o => o.Total));

        // Assert
        var provider = services.BuildServiceProvider();

        // Check mapping is registered
        var entityMapping = provider.GetService<ITenantEntityMapping<DapperOracleTenantTestOrder, Guid>>();
        entityMapping.ShouldNotBeNull();
        entityMapping.IsTenantEntity.ShouldBeTrue();
        entityMapping.TableName.ShouldBe("Orders");

        // Check repository is registered
        var repository = provider.GetService<IFunctionalRepository<DapperOracleTenantTestOrder, Guid>>();
        repository.ShouldNotBeNull();
        repository.ShouldBeOfType<TenantAwareFunctionalRepositoryDapper<DapperOracleTenantTestOrder, Guid>>();
    }

    [Fact]
    public void AddTenantAwareRepository_RegistersReadRepositoryInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = Substitute.For<IDbConnection>();

        services.AddSingleton(mockConnection);
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Options.Create(new DapperTenancyOptions()));

        // Act
        services.AddTenantAwareRepository<DapperOracleTenantTestOrder, Guid>(mapping =>
            mapping.ToTable("Orders")
                   .HasId(o => o.Id)
                   .HasTenantId(o => o.TenantId)
                   .MapProperty(o => o.CustomerId));

        // Assert
        var provider = services.BuildServiceProvider();
        var readRepository = provider.GetService<IFunctionalReadRepository<DapperOracleTenantTestOrder, Guid>>();
        readRepository.ShouldNotBeNull();
    }

    [Fact]
    public void AddTenantAwareReadRepository_RegistersOnlyReadRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = Substitute.For<IDbConnection>();

        services.AddSingleton(mockConnection);
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Options.Create(new DapperTenancyOptions()));

        // Act
        services.AddTenantAwareReadRepository<DapperOracleTenantTestOrder, Guid>(mapping =>
            mapping.ToTable("vw_OrderSummaries")
                   .HasId(o => o.Id)
                   .HasTenantId(o => o.TenantId)
                   .MapProperty(o => o.Total));

        // Assert
        var provider = services.BuildServiceProvider();

        // Read repository should be registered
        var readRepository = provider.GetService<IFunctionalReadRepository<DapperOracleTenantTestOrder, Guid>>();
        readRepository.ShouldNotBeNull();

        // Full repository should NOT be registered
        var fullRepository = provider.GetService<IFunctionalRepository<DapperOracleTenantTestOrder, Guid>>();
        fullRepository.ShouldBeNull();
    }

    [Fact]
    public void AddTenantAwareRepository_WithNonTenantEntity_CreatesNonTenantAwareMapping()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = Substitute.For<IDbConnection>();

        services.AddSingleton(mockConnection);
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Options.Create(new DapperTenancyOptions()));

        // Act - Register without HasTenantId
        services.AddTenantAwareRepository<DapperOracleTenantTestOrder, Guid>(mapping =>
            mapping.ToTable("GlobalOrders")
                   .HasId(o => o.Id)
                   .MapProperty(o => o.CustomerId));

        // Assert
        var provider = services.BuildServiceProvider();
        var entityMapping = provider.GetRequiredService<ITenantEntityMapping<DapperOracleTenantTestOrder, Guid>>();
        entityMapping.IsTenantEntity.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaDapperWithTenancy_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDapperWithTenancy(config => { }));
    }

    [Fact]
    public void AddEncinaDapperWithTenancy_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDapperWithTenancy(null!));
    }

    [Fact]
    public void AddTenantAwareRepository_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTenantAwareRepository<DapperOracleTenantTestOrder, Guid>(
                m => m.ToTable("Orders").HasId(o => o.Id)));
    }

    [Fact]
    public void AddTenantAwareRepository_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantAwareRepository<DapperOracleTenantTestOrder, Guid>(null!));
    }
}
