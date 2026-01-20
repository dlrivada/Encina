using Encina.DomainModeling;
using Encina.MongoDB;
using Encina.MongoDB.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.UnitOfWork;

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
        services.AddSingleton(Substitute.For<IMongoClient>());
        services.Configure<EncinaMongoDbOptions>(opt => opt.DatabaseName = "TestDb");

        // Act
        services.AddEncinaUnitOfWork();

        // Assert - Check the registration exists
        var registration = services.FirstOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
        registration.ShouldNotBeNull();
        registration.ImplementationType.ShouldBe(typeof(UnitOfWorkMongoDB));
        registration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaUnitOfWork_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IMongoClient>());
        services.Configure<EncinaMongoDbOptions>(opt => opt.DatabaseName = "TestDb");

        // Act
        var result = services.AddEncinaUnitOfWork();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaUnitOfWork_CalledTwice_DoesNotDuplicateRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IMongoClient>());
        services.Configure<EncinaMongoDbOptions>(opt => opt.DatabaseName = "TestDb");

        // Act
        services.AddEncinaUnitOfWork();
        services.AddEncinaUnitOfWork();

        // Assert - Only one IUnitOfWork registration
        var registrations = services.Where(d => d.ServiceType == typeof(IUnitOfWork)).ToList();
        registrations.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaUnitOfWork_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services!.AddEncinaUnitOfWork());
    }

    #endregion
}
