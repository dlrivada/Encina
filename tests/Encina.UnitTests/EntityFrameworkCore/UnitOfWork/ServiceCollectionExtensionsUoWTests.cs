using Encina.DomainModeling;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Unit tests for UnitOfWork registration in ServiceCollectionExtensions.
/// </summary>
[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsUoWTests
{
    #region AddEncinaUnitOfWork Tests

    [Fact]
    public void AddEncinaUnitOfWork_RegistersIUnitOfWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestUoWDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaUnitOfWork<TestUoWDbContext>();

        // Assert - Check the registration exists
        var registration = services.FirstOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
        registration.ShouldNotBeNull();
        registration.ImplementationType.ShouldBe(typeof(UnitOfWorkEF));
        registration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaUnitOfWork_RegistersDbContextAsNonGeneric()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestUoWDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaUnitOfWork<TestUoWDbContext>();

        // Assert - Check the DbContext registration exists
        var registration = services.FirstOrDefault(d => d.ServiceType == typeof(DbContext));
        registration.ShouldNotBeNull();
        registration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaUnitOfWork_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestUoWDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        var result = services.AddEncinaUnitOfWork<TestUoWDbContext>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaUnitOfWork_IUnitOfWorkIsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestUoWDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaUnitOfWork<TestUoWDbContext>();

        // Assert - Check that IUnitOfWork is registered as Scoped
        var registration = services.FirstOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
        registration.ShouldNotBeNull();
        registration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaUnitOfWork_CalledTwice_DoesNotDuplicateRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestUoWDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaUnitOfWork<TestUoWDbContext>();
        services.AddEncinaUnitOfWork<TestUoWDbContext>();

        // Assert - Only one IUnitOfWork registration
        var registrations = services.Where(d => d.ServiceType == typeof(IUnitOfWork)).ToList();
        registrations.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaUnitOfWork_WorksWithEncinaEntityFrameworkCore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestUoWDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act - Both methods should work together
        services.AddEncinaEntityFrameworkCore<TestUoWDbContext>();
        services.AddEncinaUnitOfWork<TestUoWDbContext>();

        // Assert - Both registrations exist
        var uowRegistration = services.FirstOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
        var dbContextRegistration = services.FirstOrDefault(d => d.ServiceType == typeof(DbContext));

        uowRegistration.ShouldNotBeNull();
        dbContextRegistration.ShouldNotBeNull();
    }

    #endregion
}

/// <summary>
/// Test DbContext for ServiceCollection extension tests.
/// </summary>
public class TestUoWDbContext : DbContext
{
    public TestUoWDbContext(DbContextOptions<TestUoWDbContext> options) : base(options)
    {
    }

    public DbSet<TestUoWServiceEntity> TestEntities { get; set; } = null!;
}

/// <summary>
/// Test entity for ServiceCollection tests.
/// </summary>
public class TestUoWServiceEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
