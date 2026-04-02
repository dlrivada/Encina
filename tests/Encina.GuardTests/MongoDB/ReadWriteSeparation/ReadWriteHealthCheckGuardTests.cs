using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.ReadWriteSeparation;

public class ReadWriteHealthCheckGuardTests
{
    private static readonly IMongoClient Client = Substitute.For<IMongoClient>();
    private static readonly IOptions<EncinaMongoDbOptions> Opts =
        Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" });

    [Fact]
    public void Ctor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoHealthCheck(null!, Opts));

    [Fact]
    public void Ctor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoHealthCheck(Client, null!));
}
