using Encina.Messaging.Transactions;
using Encina.MongoDB.Transactions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Transactions;

public class MongoSessionAccessorGuardTests
{
    [Fact]
    public void Constructor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MongoSessionAccessor(null!, Substitute.For<ILogger<MongoSessionAccessor>>()));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MongoSessionAccessor(Substitute.For<IMongoClient>(), null!));

    [Fact]
    public void CommitFailed_NullException_Throws()
        => Should.Throw<ArgumentNullException>(() => TransactionErrors.CommitFailed(null!, "label"));
}
