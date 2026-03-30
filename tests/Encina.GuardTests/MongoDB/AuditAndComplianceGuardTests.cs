using Encina.MongoDB;
using Encina.MongoDB.ABAC;
using Encina.MongoDB.Anonymization;
using Encina.MongoDB.Auditing;
using Encina.MongoDB.ProcessingActivity;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB;

public class AuditAndComplianceGuardTests
{
    private static readonly IOptions<EncinaMongoDbOptions> Opts = Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" });

    #region PolicyStoreMongo

    [Fact]
    public void PolicyStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new PolicyStoreMongo(null!, Opts));

    [Fact]
    public void PolicyStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new PolicyStoreMongo(Substitute.For<IMongoClient>(), null!));

    #endregion

    #region AuditStoreMongoDB

    [Fact]
    public void AuditStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new AuditStoreMongoDB(null!, Opts, NullLogger<AuditStoreMongoDB>.Instance));

    [Fact]
    public void AuditStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new AuditStoreMongoDB(Substitute.For<IMongoClient>(), null!, NullLogger<AuditStoreMongoDB>.Instance));

    [Fact]
    public void AuditStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new AuditStoreMongoDB(Substitute.For<IMongoClient>(), Opts, null!));

    #endregion

    #region ReadAuditStoreMongoDB

    [Fact]
    public void ReadAuditStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadAuditStoreMongoDB(null!, Opts, NullLogger<ReadAuditStoreMongoDB>.Instance));

    [Fact]
    public void ReadAuditStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadAuditStoreMongoDB(Substitute.For<IMongoClient>(), null!, NullLogger<ReadAuditStoreMongoDB>.Instance));

    [Fact]
    public void ReadAuditStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadAuditStoreMongoDB(Substitute.For<IMongoClient>(), Opts, null!));

    #endregion

    #region AuditLogStoreMongoDB

    [Fact]
    public void AuditLogStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new AuditLogStoreMongoDB(null!, Opts, NullLogger<AuditLogStoreMongoDB>.Instance));

    [Fact]
    public void AuditLogStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new AuditLogStoreMongoDB(Substitute.For<IMongoClient>(), null!, NullLogger<AuditLogStoreMongoDB>.Instance));

    [Fact]
    public void AuditLogStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new AuditLogStoreMongoDB(Substitute.For<IMongoClient>(), Opts, null!));

    #endregion

    #region TokenMappingStoreMongoDB

    [Fact]
    public void TokenMappingStore_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TokenMappingStoreMongoDB(null!, Opts, NullLogger<TokenMappingStoreMongoDB>.Instance));

    [Fact]
    public void TokenMappingStore_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TokenMappingStoreMongoDB(Substitute.For<IMongoClient>(), null!, NullLogger<TokenMappingStoreMongoDB>.Instance));

    [Fact]
    public void TokenMappingStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TokenMappingStoreMongoDB(Substitute.For<IMongoClient>(), Opts, null!));

    #endregion

    #region ProcessingActivityRegistryMongoDB

    [Fact]
    public void ProcessingActivity_NullConnectionString_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB(null!, "db"));

    [Fact]
    public void ProcessingActivity_EmptyConnectionString_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB("", "db"));

    [Fact]
    public void ProcessingActivity_NullDatabaseName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB("mongodb://localhost", null!));

    [Fact]
    public void ProcessingActivity_EmptyDatabaseName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB("mongodb://localhost", ""));

    #endregion
}
