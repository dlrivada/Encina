using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DbTokenMappingStore = Encina.EntityFrameworkCore.Anonymization.TokenMappingStoreEF;

namespace Encina.UnitTests.EntityFrameworkCore;

/// <summary>
/// Verifies that the database-backed <see cref="ITokenMappingStore"/> wins over
/// <see cref="InMemoryTokenMappingStore"/> regardless of the order in which
/// <c>AddEncinaAnonymization</c> and <c>AddEncinaEntityFrameworkCore</c> run (#1295).
/// </summary>
public sealed class TokenMappingStoreRegistrationTests
{
    [Fact]
    public void AddEncinaAnonymization_ThenAddEncinaEntityFrameworkCore_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaAnonymization();
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseAnonymization = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITokenMappingStore>()
            .ShouldBeOfType<DbTokenMappingStore>();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_ThenAddEncinaAnonymization_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseAnonymization = true);
        services.AddEncinaAnonymization();

        // Assert
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITokenMappingStore>()
            .ShouldBeOfType<DbTokenMappingStore>();
    }

    [Fact]
    public void CustomTokenMappingStore_RegisteredBeforeProvider_StaysResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var customStore = Substitute.For<ITokenMappingStore>();
        services.AddSingleton(customStore);

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseAnonymization = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITokenMappingStore>().ShouldBeSameAs(customStore);
    }
}
