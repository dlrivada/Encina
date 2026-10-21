using System.Globalization;
using Encina.Recoverability;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;
using MySqlConnector;
using Npgsql;

namespace Encina.IntegrationTests.Recoverability;

/// <summary>
/// A pool of size one with a short timeout, exhausted against each real database driver: the default classifier
/// reads the driver's exception as <see cref="ErrorClassification.Transient"/> although a driver can raise it as
/// an <see cref="InvalidOperationException"/> (permanent by type) (#1223, decision D-C).
/// </summary>
internal static class PoolExhaustion
{
    internal static readonly DefaultErrorClassifier Classifier = new();

    internal static void ShouldBeTransient(Exception exception)
    {
        var detail = $"{exception.GetType().FullName}";
        Classifier.Classify(EncinaError.New(exception), exception).ShouldBe(ErrorClassification.Transient, detail);
        Classifier.Classify(EncinaError.New("wrapped", new InvalidOperationException("outer", exception)), null)
            .ShouldBe(ErrorClassification.Transient, detail);
    }
}

[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class SqlServerPoolExhaustionTests
{
    private readonly SqlServerFixture _fixture;

    public SqlServerPoolExhaustionTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExhaustedPool_IsTransient()
    {
        // The driver's messages are localized: the classifier matches the English text.
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var connectionString = new SqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            MaxPoolSize = 1,
            ConnectTimeout = 5,
            Pooling = true,
            ApplicationName = "encina-pool-exhaustion"
        }.ConnectionString;
        await using var held = new SqlConnection(connectionString);
        await held.OpenAsync(TestContext.Current.CancellationToken);
        await using var second = new SqlConnection(connectionString);

        var exception = await Should.ThrowAsync<Exception>(() => second.OpenAsync(TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("max pool size was reached", Case.Insensitive, $"{exception.GetType().FullName}: {exception.Message}");
        PoolExhaustion.ShouldBeTransient(exception);
    }
}

[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class PostgreSqlPoolExhaustionTests
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlPoolExhaustionTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExhaustedPool_IsTransient()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            MaxPoolSize = 1,
            Timeout = 1,
            Pooling = true,
            ApplicationName = "encina-pool-exhaustion"
        }.ConnectionString;
        await using var held = new NpgsqlConnection(connectionString);
        await held.OpenAsync(TestContext.Current.CancellationToken);
        await using var second = new NpgsqlConnection(connectionString);

        var exception = await Should.ThrowAsync<Exception>(() => second.OpenAsync(TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("connection pool has been exhausted", Case.Insensitive);
        PoolExhaustion.ShouldBeTransient(exception);
    }
}

[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("ADO-MySQL")]
public sealed class MySqlPoolExhaustionTests
{
    private readonly MySqlFixture _fixture;

    public MySqlPoolExhaustionTests(MySqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExhaustedPool_IsTransient()
    {
        var connectionString = new MySqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            MaximumPoolSize = 1,
            ConnectionTimeout = 1,
            Pooling = true,
            ApplicationName = "encina-pool-exhaustion"
        }.ConnectionString;
        await using var held = new MySqlConnection(connectionString);
        await held.OpenAsync(TestContext.Current.CancellationToken);
        await using var second = new MySqlConnection(connectionString);

        var exception = await Should.ThrowAsync<Exception>(() => second.OpenAsync(TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("all pooled connections are in use", Case.Insensitive);
        PoolExhaustion.ShouldBeTransient(exception);
    }
}

[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
[Collection(MongoDbCollection.Name)]
public sealed class MongoDbPoolExhaustionTests
{
    private readonly MongoDbFixture _fixture;

    public MongoDbPoolExhaustionTests(MongoDbFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExhaustedPool_IsTransient()
    {
        var settings = MongoClientSettings.FromConnectionString(_fixture.ConnectionString);
        settings.MaxConnectionPoolSize = 1;
        settings.WaitQueueTimeout = TimeSpan.FromMilliseconds(300);
        var client = new MongoClient(settings);
        var collection = client.GetDatabase(MongoDbFixture.DatabaseName).GetCollection<BsonDocument>("pool_exhaustion");
        await collection.InsertOneAsync(new BsonDocument("x", 1), cancellationToken: TestContext.Current.CancellationToken);

        // One slow operation holds the only pooled connection; a second one waits for it and times out.
        var slow = collection.Find(new BsonDocument("$where", "sleep(3000) || true")).ToListAsync(TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);

        var exception = await Should.ThrowAsync<Exception>(
            () => collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync(TestContext.Current.CancellationToken));

        PoolExhaustion.Classifier.Classify(EncinaError.New(exception), exception)
            .ShouldBe(ErrorClassification.Transient, $"{exception.GetType().FullName}: {exception.Message}");
        try
        {
            await slow;
        }
        catch (MongoException)
        {
            // the slow operation is not what is under test
        }
    }
}
