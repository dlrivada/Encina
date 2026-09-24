using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;
using Encina.MongoDB;
using DbTokenMappingStore = Encina.MongoDB.Anonymization.TokenMappingStoreMongoDB;

namespace Encina.UnitTests.MongoDB;

/// <summary>
/// Verifies that the database-backed <see cref="ITokenMappingStore"/> wins over
/// <see cref="InMemoryTokenMappingStore"/> regardless of the order in which
/// <c>AddEncinaAnonymization</c> and <c>AddEncinaMongoDB</c> run (#1295).
/// </summary>
public sealed class TokenMappingStoreRegistrationTests
{
    [Fact]
    public void AddEncinaAnonymization_ThenAddEncinaMongoDB_RegistersDatabaseStoreLast()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAnonymization();
        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseAnonymization = true;
        });

        // Assert: the in-memory default was removed, only the database-backed store remains.
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenMappingStore>()
            .ShouldBeOfType<DbTokenMappingStore>();
    }

    [Fact]
    public void AddEncinaMongoDB_ThenAddEncinaAnonymization_RegistersDatabaseStoreOnly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseAnonymization = true;
        });
        services.AddEncinaAnonymization();

        // Assert: AddEncinaAnonymization's TryAdd no-ops because a store is already registered.
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenMappingStore>()
            .ShouldBeOfType<DbTokenMappingStore>();
    }

    [Fact]
    public void CustomTokenMappingStore_RegisteredBeforeProvider_StaysResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var customStore = Substitute.For<ITokenMappingStore>();
        services.AddSingleton(customStore);

        // Act
        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.UseAnonymization = true;
        });

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITokenMappingStore>().ShouldBeSameAs(customStore);
    }
}
