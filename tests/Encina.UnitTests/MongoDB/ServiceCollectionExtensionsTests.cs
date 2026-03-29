using Encina.MongoDB;

namespace Encina.UnitTests.MongoDB;

/// <summary>
/// Unit tests for MongoDB <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMongoDB_NullServices_ShouldThrow()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaMongoDB(opts => opts.ConnectionString = "mongodb://localhost"));
    }

    [Fact]
    public void AddEncinaMongoDB_NullConfigure_ShouldThrow()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaMongoDB(null!));
    }

    [Fact]
    public void AddEncinaMongoDB_ReturnsSameCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost";
            opts.DatabaseName = "test";
        });

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaMongoDB_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaMongoDB(opts =>
        {
            opts.ConnectionString = "mongodb://localhost:27017";
            opts.DatabaseName = "encina-test";
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetService<IOptions<EncinaMongoDbOptions>>();
        options.ShouldNotBeNull();
        options.Value.ConnectionString.ShouldBe("mongodb://localhost:27017");
        options.Value.DatabaseName.ShouldBe("encina-test");
    }
}
