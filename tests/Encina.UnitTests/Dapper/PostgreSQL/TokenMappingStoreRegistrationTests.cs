using System.Data;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.Dapper.PostgreSQL;
using DbTokenMappingStore = Encina.Dapper.PostgreSQL.Anonymization.TokenMappingStoreDapper;

namespace Encina.UnitTests.Dapper.PostgreSQL;

/// <summary>
/// Verifies that the database-backed <see cref="ITokenMappingStore"/> wins over
/// <see cref="InMemoryTokenMappingStore"/> regardless of the order in which
/// <c>AddEncinaAnonymization</c> and <c>AddEncinaDapper</c> run (#1295).
/// </summary>
public sealed class TokenMappingStoreRegistrationTests
{
    [Fact]
    public void AddEncinaAnonymization_ThenAddEncinaDapper_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaAnonymization();
        services.AddEncinaDapper(config => config.UseAnonymization = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenMappingStore>()
            .ShouldBeOfType<DbTokenMappingStore>();
    }

    [Fact]
    public void AddEncinaDapper_ThenAddEncinaAnonymization_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaDapper(config => config.UseAnonymization = true);
        services.AddEncinaAnonymization();

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenMappingStore>()
            .ShouldBeOfType<DbTokenMappingStore>();
    }

    [Fact]
    public void CustomTokenMappingStore_RegisteredBeforeProvider_StaysResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());
        var customStore = Substitute.For<ITokenMappingStore>();
        services.AddSingleton(customStore);

        // Act
        services.AddEncinaDapper(config => config.UseAnonymization = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenMappingStore>().ShouldBeSameAs(customStore);
    }
}
