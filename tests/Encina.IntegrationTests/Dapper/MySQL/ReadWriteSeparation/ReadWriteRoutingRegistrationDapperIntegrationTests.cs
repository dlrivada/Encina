using System.Data;
using Encina.Dapper.MySQL;
using Encina.Dapper.MySQL.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Encina.TestInfrastructure.Fixtures;
using Encina.Testing.Shouldly;
using LanguageExt;
using MySqlConnector;

namespace Encina.IntegrationTests.Dapper.MySQL.ReadWriteSeparation;

/// <summary>
/// Proves, on a real MySQL database, that <c>AddEncinaDapper</c> with <c>UseReadWriteSeparation</c> registers
/// the routing behavior and the read/write services, and that the registered behavior sends a query to the read
/// database and a command to the write database (#2029). The read database is a second database of the same
/// container, tagged with a marker row, so the connection a request used can be told apart.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("Dapper-MySQL")]
public sealed class ReadWriteRoutingRegistrationDapperIntegrationTests : IAsyncLifetime
{
    private const string ReadDatabase = "rw_routing_read_2029";
    private const string MarkerSql = "SELECT source FROM rw_routing_marker";

    private readonly MySqlFixture _fixture;
    private string _readConnectionString = string.Empty;

    public ReadWriteRoutingRegistrationDapperIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        await using (var admin = new MySqlConnection(_fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await ExecuteAsync(admin, $"DROP DATABASE IF EXISTS {ReadDatabase}");
            await ExecuteAsync(admin, $"CREATE DATABASE {ReadDatabase}");
            await ExecuteAsync(admin, "DROP TABLE IF EXISTS rw_routing_marker");
            await ExecuteAsync(admin, "CREATE TABLE rw_routing_marker (source VARCHAR(10) NOT NULL)");
            await ExecuteAsync(admin, "INSERT INTO rw_routing_marker (source) VALUES ('write')");
        }

        _readConnectionString = new MySqlConnectionStringBuilder(_fixture.ConnectionString) { Database = ReadDatabase }
            .ConnectionString;

        await using var replica = new MySqlConnection(_readConnectionString);
        await replica.OpenAsync();
        await ExecuteAsync(replica, "CREATE TABLE rw_routing_marker (source VARCHAR(10) NOT NULL)");
        await ExecuteAsync(replica, "INSERT INTO rw_routing_marker (source) VALUES ('read')");
    }

    public async ValueTask DisposeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        await MySqlConnection.ClearAllPoolsAsync();
        await using var admin = new MySqlConnection(_fixture.ConnectionString);
        await admin.OpenAsync();
        await ExecuteAsync(admin, $"DROP DATABASE IF EXISTS {ReadDatabase}");
        await ExecuteAsync(admin, "DROP TABLE IF EXISTS rw_routing_marker");
    }

    [Fact]
    public async Task Query_IsRoutedToTheReadDatabase()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container is not available");
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var result = await Send(scope.ServiceProvider, new RwQuery());

        result.ShouldBeRight().ShouldBe("read");
    }

    [Fact]
    public async Task Command_IsRoutedToTheWriteDatabase()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container is not available");
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var result = await Send(scope.ServiceProvider, new RwCommand());

        result.ShouldBeRight().ShouldBe("write");
    }

    [Fact]
    public async Task QueryMarkedForceWrite_IsRoutedToTheWriteDatabase()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL container is not available");
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var result = await Send(scope.ServiceProvider, new RwForceWriteQuery());

        result.ShouldBeRight().ShouldBe("write");
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDapper(
            _ => new MySqlConnection(_fixture.ConnectionString),
            config =>
            {
                config.UseReadWriteSeparation = true;
                config.ReadWriteSeparationOptions.WriteConnectionString = _fixture.ConnectionString;
                config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(_readConnectionString);
            });

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    /// <summary>
    /// Runs the registered routing behavior for <typeparamref name="TRequest"/> around a handler that reads the
    /// marker through the registered <see cref="IReadWriteConnectionFactory"/>, as a handler of the application would.
    /// </summary>
    private static async Task<Either<EncinaError, string>> Send<TRequest>(IServiceProvider scopeProvider, TRequest request)
        where TRequest : IRequest<string>
    {
        var behavior = scopeProvider.GetServices<IPipelineBehavior<TRequest, string>>()
            .OfType<ReadWriteRoutingPipelineBehavior<TRequest, string>>()
            .Single();
        var factory = scopeProvider.GetRequiredService<IReadWriteConnectionFactory>();

        return await behavior.Handle(
            request,
            Substitute.For<IRequestContext>(),
            async () =>
            {
                var connection = (await factory.CreateConnectionAsync()).ShouldBeRight();
                using (connection)
                {
                    var source = await global::Dapper.SqlMapper.QuerySingleAsync<string>(connection, MarkerSql);
                    return Prelude.Right<EncinaError, string>(source);
                }
            },
            CancellationToken.None);
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string sql)
    {
        await global::Dapper.SqlMapper.ExecuteAsync(connection, sql);
    }

    private sealed record RwQuery : IQuery<string>;

    private sealed record RwCommand : ICommand<string>;

    [ForceWriteDatabase(Reason = "Must read the latest value after a write")]
    private sealed record RwForceWriteQuery : IQuery<string>;
}
