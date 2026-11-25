using Encina.MongoDB.ProcessingActivity;
using MongoDB.Driver;
using NSubstitute;

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

    [Fact]
    public void Ctor_NullCollection_ThrowsWithParamName()
        => Should.Throw<ArgumentNullException>(() => new ProcessingActivityRegistryMongoDB(null!))
            .ParamName.ShouldBe("collection");

    #endregion

    #region Methods

    private static ProcessingActivityRegistryMongoDB NewRegistry()
        => new(Substitute.For<IMongoCollection<ProcessingActivityDocument>>());

    [Fact]
    public async Task RegisterActivityAsync_NullActivity_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await NewRegistry().RegisterActivityAsync(null!));

        ex.ParamName.ShouldBe("activity");
    }

    [Fact]
    public async Task UpdateActivityAsync_NullActivity_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await NewRegistry().UpdateActivityAsync(null!));

        ex.ParamName.ShouldBe("activity");
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NullRequestType_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await NewRegistry().GetActivityByRequestTypeAsync(null!));

        ex.ParamName.ShouldBe("requestType");
    }

    #endregion
}
