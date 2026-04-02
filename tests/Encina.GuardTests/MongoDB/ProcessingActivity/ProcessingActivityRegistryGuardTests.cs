using Encina.MongoDB.ProcessingActivity;

namespace Encina.GuardTests.MongoDB.ProcessingActivity;

public class ProcessingActivityRegistryGuardTests
{
    #region Constructor

    [Fact]
    public void Ctor_NullConnectionString_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB(null!, "db"));

    [Fact]
    public void Ctor_EmptyConnectionString_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB("", "db"));

    [Fact]
    public void Ctor_WhitespaceConnectionString_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB("   ", "db"));

    [Fact]
    public void Ctor_NullDatabaseName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB("mongodb://localhost:27017", null!));

    [Fact]
    public void Ctor_EmptyDatabaseName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB("mongodb://localhost:27017", ""));

    [Fact]
    public void Ctor_WhitespaceDatabaseName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ProcessingActivityRegistryMongoDB("mongodb://localhost:27017", "   "));

    #endregion
}
