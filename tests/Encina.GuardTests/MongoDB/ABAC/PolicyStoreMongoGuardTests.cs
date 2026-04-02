using Encina.MongoDB;
using Encina.MongoDB.ABAC;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.ABAC;

public class PolicyStoreMongoGuardTests
{
    private static readonly IOptions<EncinaMongoDbOptions> Opts =
        Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" });

    #region Constructor

    [Fact]
    public void Ctor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new PolicyStoreMongo(null!, Opts));

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        var client = CreateMockClient();
        Should.Throw<ArgumentNullException>(() =>
            new PolicyStoreMongo(client, null!));
    }

    #endregion

    // Note: Method guard tests for GetPolicySetAsync, SavePolicySetAsync, etc. require a fully
    // constructed PolicyStoreMongo which needs BSON class maps registered. The constructor guards
    // are the primary coverage target. Method guards (ArgumentException/ArgumentNullException) are
    // documented but deferred to integration tests where a real MongoDB connection exists.

    private static IMongoClient CreateMockClient()
    {
        var client = Substitute.For<IMongoClient>();
        var db = Substitute.For<IMongoDatabase>();

        // Configure with multiple overloads to handle both GetDatabase(string) and GetDatabase(string, settings)
        client.GetDatabase(Arg.Any<string>()).Returns(db);
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(db);

        return client;
    }
}
