using Encina.MongoDB;
using Encina.MongoDB.UnitOfWork;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB;

public class UnitOfWorkMongoDBGuardTests
{
    [Fact]
    public void Constructor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkMongoDB(null!, Options.Create(new EncinaMongoDbOptions()), Substitute.For<IServiceProvider>()));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkMongoDB(Substitute.For<IMongoClient>(), null!, Substitute.For<IServiceProvider>()));

    [Fact]
    public void Constructor_NullServiceProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkMongoDB(Substitute.For<IMongoClient>(), Options.Create(new EncinaMongoDbOptions()), null!));
}
