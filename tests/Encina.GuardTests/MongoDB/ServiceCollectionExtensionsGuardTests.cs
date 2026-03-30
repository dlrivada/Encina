using Encina.MongoDB;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB;

public class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaMongoDB_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaMongoDB(_ => { }));

    [Fact]
    public void AddEncinaMongoDB_NullConfigure_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaMongoDB(null!));

    [Fact]
    public void AddEncinaMongoDB_WithClient_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaMongoDB(Substitute.For<IMongoClient>(), _ => { }));

    [Fact]
    public void AddEncinaMongoDB_WithClient_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaMongoDB((IMongoClient)null!, _ => { }));

    [Fact]
    public void AddEncinaMongoDB_WithClient_NullConfigure_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaMongoDB(Substitute.For<IMongoClient>(), null!));

}
