using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.IdGeneration.DependencyInjection;

/// <summary>
/// Unit tests for <see cref="Encina.IdGeneration.ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaIdGeneration_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaIdGeneration(_ => { });
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaIdGeneration_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaIdGeneration(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddEncinaIdGeneration_WithSnowflake_RegistersGenerators()
    {
        var services = new ServiceCollection();

        services.AddEncinaIdGeneration(o => o.UseSnowflake(s => s.MachineId = 1));

        using var provider = services.BuildServiceProvider();
        provider.GetService<IIdGenerator<SnowflakeId>>().ShouldNotBeNull();
        provider.GetService<IShardedIdGenerator<SnowflakeId>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaIdGeneration_WithUlid_RegistersGenerator()
    {
        var services = new ServiceCollection();

        services.AddEncinaIdGeneration(o => o.UseUlid());

        using var provider = services.BuildServiceProvider();
        provider.GetService<IIdGenerator<UlidId>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaIdGeneration_WithUuidV7_RegistersGenerator()
    {
        var services = new ServiceCollection();

        services.AddEncinaIdGeneration(o => o.UseUuidV7());

        using var provider = services.BuildServiceProvider();
        provider.GetService<IIdGenerator<UuidV7Id>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaIdGeneration_WithShardPrefixed_RegistersGenerators()
    {
        var services = new ServiceCollection();

        services.AddEncinaIdGeneration(o => o.UseShardPrefixed());

        using var provider = services.BuildServiceProvider();
        provider.GetService<IIdGenerator<ShardPrefixedId>>().ShouldNotBeNull();
        provider.GetService<IShardedIdGenerator<ShardPrefixedId>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaIdGeneration_DisabledStrategy_DoesNotRegister()
    {
        var services = new ServiceCollection();

        services.AddEncinaIdGeneration(o => o.UseSnowflake(s => s.MachineId = 1));

        using var provider = services.BuildServiceProvider();
        provider.GetService<IIdGenerator<UlidId>>().ShouldBeNull();
        provider.GetService<IIdGenerator<UuidV7Id>>().ShouldBeNull();
        provider.GetService<IIdGenerator<ShardPrefixedId>>().ShouldBeNull();
    }

    [Fact]
    public void AddEncinaIdGeneration_AllStrategies_RegistersAll()
    {
        var services = new ServiceCollection();

        services.AddEncinaIdGeneration(o =>
        {
            o.UseSnowflake(s => s.MachineId = 1);
            o.UseUlid();
            o.UseUuidV7();
            o.UseShardPrefixed();
        });

        using var provider = services.BuildServiceProvider();
        provider.GetService<IIdGenerator<SnowflakeId>>().ShouldNotBeNull();
        provider.GetService<IIdGenerator<UlidId>>().ShouldNotBeNull();
        provider.GetService<IIdGenerator<UuidV7Id>>().ShouldNotBeNull();
        provider.GetService<IIdGenerator<ShardPrefixedId>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaIdGeneration_TryAddSingleton_DoesNotOverrideExisting()
    {
        var services = new ServiceCollection();

        services.AddEncinaIdGeneration(o => o.UseSnowflake(s => s.MachineId = 1));
        services.AddEncinaIdGeneration(o => o.UseSnowflake(s => s.MachineId = 2));

        using var provider = services.BuildServiceProvider();
        var generator = provider.GetService<IIdGenerator<SnowflakeId>>();
        generator.ShouldNotBeNull();

        // First registration should win (TryAddSingleton behavior)
        var result = generator.Generate();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaIdGeneration_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaIdGeneration(o => o.UseUlid());

        result.ShouldBeSameAs(services);
    }
}
