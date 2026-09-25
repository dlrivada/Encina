using System.Data;
using Encina.ADO.MySQL;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using DbTokenMappingStore = Encina.ADO.MySQL.Anonymization.TokenMappingStoreADO;

namespace Encina.UnitTests.ADO.MySQL;

/// <summary>
/// Verifies that the database-backed <see cref="ITokenMappingStore"/> wins over
/// <see cref="InMemoryTokenMappingStore"/> regardless of the order in which
/// <c>AddEncinaAnonymization</c> and <c>AddEncinaADO</c> run (#1295).
/// </summary>
public sealed class TokenMappingStoreRegistrationTests
{
    [Fact]
    public void AddEncinaAnonymization_ThenAddEncinaADO_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaAnonymization();
        services.AddEncinaADO(config => config.UseAnonymization = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenMappingStore>()
            .ShouldBeOfType<DbTokenMappingStore>();
    }

    [Fact]
    public void AddEncinaADO_ThenAddEncinaAnonymization_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaADO(config => config.UseAnonymization = true);
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
        services.AddEncinaADO(config => config.UseAnonymization = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenMappingStore>().ShouldBeSameAs(customStore);
    }
}
