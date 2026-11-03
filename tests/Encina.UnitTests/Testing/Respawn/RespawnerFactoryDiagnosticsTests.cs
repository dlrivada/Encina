using Encina.Testing.Respawn;

namespace Encina.UnitTests.Testing.Respawn;

/// <summary>
/// Serializes every test that touches the static diagnostics switches of
/// <see cref="RespawnerFactory"/>.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RespawnerFactoryStaticStateDefinition
{
    public const string Name = "RespawnerFactory static state";
}

/// <summary>
/// Tests for the connection string parsing branches and the verbose diagnostics of
/// <see cref="RespawnerFactory"/>.
/// </summary>
[Collection(RespawnerFactoryStaticStateDefinition.Name)]
public sealed class RespawnerFactoryDiagnosticsTests : IDisposable
{
    // Unknown keywords make every provider's connection string builder throw.
    private const string UnparsableConnectionString = "Host=localhost;NoSuchKeyword=1";

    private readonly bool _verboseBefore = RespawnerFactory.VerboseDiagnostics;
    private readonly Action<string, string, string?>? _callbackBefore = RespawnerFactory.DiagnosticsCallback;

    public void Dispose()
    {
        RespawnerFactory.VerboseDiagnostics = _verboseBefore;
        RespawnerFactory.DiagnosticsCallback = _callbackBefore;
    }

    [Fact]
    public void VerboseDiagnostics_RoundTripsTheAssignedValue()
    {
        // Act
        RespawnerFactory.VerboseDiagnostics = true;
        var enabled = RespawnerFactory.VerboseDiagnostics;
        RespawnerFactory.VerboseDiagnostics = false;
        var disabled = RespawnerFactory.VerboseDiagnostics;

        // Assert
        enabled.ShouldBeTrue();
        disabled.ShouldBeFalse();
    }

    [Fact]
    public void DiagnosticsCallback_RoundTripsTheAssignedValue()
    {
        // Arrange
        Action<string, string, string?> callback = (_, _, _) => { };

        // Act
        RespawnerFactory.DiagnosticsCallback = callback;

        // Assert
        RespawnerFactory.DiagnosticsCallback.ShouldBeSameAs(callback);
    }

    [Fact]
    public void InferAdapter_UnparsableConnectionStringWithVerbose_ReportsEveryProviderThatFailedToParse()
    {
        // Arrange
        var providers = new List<string>();
        RespawnerFactory.VerboseDiagnostics = true;
        RespawnerFactory.DiagnosticsCallback = (provider, message, _) =>
        {
            lock (providers)
            {
                providers.Add(provider);
            }

            message.ShouldNotBeNullOrWhiteSpace();
        };

        // Act
        var adapter = RespawnerFactory.InferAdapter(UnparsableConnectionString);

        // Assert
        adapter.ShouldBeNull();
        providers.ShouldBe(["PostgreSQL", "SqlServer", "MySQL"]);
    }

    [Fact]
    public void InferAdapter_UnparsableConnectionStringWithoutVerbose_DoesNotInvokeTheCallback()
    {
        // Arrange
        var invoked = false;
        RespawnerFactory.VerboseDiagnostics = false;
        RespawnerFactory.DiagnosticsCallback = (_, _, _) => invoked = true;

        // Act
        var adapter = RespawnerFactory.InferAdapter(UnparsableConnectionString);

        // Assert
        adapter.ShouldBeNull();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public void InferAdapter_UnparsableConnectionStringWithVerboseAndNoCallback_StillReturnsNull()
    {
        // Arrange
        RespawnerFactory.VerboseDiagnostics = true;
        RespawnerFactory.DiagnosticsCallback = null;

        // Act
        var adapter = RespawnerFactory.InferAdapter(UnparsableConnectionString);

        // Assert
        adapter.ShouldBeNull();
    }

    [Theory]
    [InlineData("Server=localhost", RespawnAdapter.SqlServer)]
    [InlineData("Server=db-host;Database=app;Port=3306", RespawnAdapter.MySql)]
    [InlineData("Server=db-host;Database=app;User=root;Password=secret", RespawnAdapter.MySql)]
    [InlineData("Server=db-host;Database=app;Uid=root;Password=secret", RespawnAdapter.SqlServer)]
    [InlineData("Server=db-host;Database=app;Trusted_Connection=True", RespawnAdapter.SqlServer)]
    public void InferAdapter_AmbiguousKeywords_ResolvesBySqlServerHeuristics(string connectionString, RespawnAdapter expected)
    {
        // Act
        var adapter = RespawnerFactory.InferAdapter(connectionString);

        // Assert
        adapter.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Server=test.db")]
    [InlineData("Server=:memory:")]
    public void InferAdapter_SqliteStyleServerValues_AreNotClaimedByAnyServerProvider(string connectionString)
    {
        // Act
        var adapter = RespawnerFactory.InferAdapter(connectionString);

        // Assert
        adapter.ShouldBeNull();
    }

    [Fact]
    public void InferAdapter_ConnectionStringWithoutAnyServer_ReturnsNull()
    {
        // Act
        var adapter = RespawnerFactory.InferAdapter("Database=app;Password=secret");

        // Assert
        adapter.ShouldBeNull();
    }
}
