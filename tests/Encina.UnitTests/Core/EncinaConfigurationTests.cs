using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Core;

public sealed class EncinaConfigurationTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new EncinaConfiguration();

        config.HandlerLifetime.ShouldBe(ServiceLifetime.Scoped);
        config.NotificationDispatch.ShouldNotBeNull();
        config.NotificationDispatch.Strategy.ShouldBe(NotificationDispatchStrategy.Sequential);
    }

    [Fact]
    public void WithHandlerLifetime_SetsLifetime()
    {
        var config = new EncinaConfiguration();

        config.WithHandlerLifetime(ServiceLifetime.Transient);

        config.HandlerLifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void WithHandlerLifetime_ReturnsConfigurationForChaining()
    {
        var config = new EncinaConfiguration();

        var result = config.WithHandlerLifetime(ServiceLifetime.Singleton);

        result.ShouldBe(config);
    }

    [Fact]
    public void UseParallelNotificationDispatch_SetsStrategy()
    {
        var config = new EncinaConfiguration();

        config.UseParallelNotificationDispatch(NotificationDispatchStrategy.ParallelWhenAll, 4);

        config.NotificationDispatch.Strategy.ShouldBe(NotificationDispatchStrategy.ParallelWhenAll);
        config.NotificationDispatch.MaxDegreeOfParallelism.ShouldBe(4);
    }

    [Fact]
    public void UseParallelNotificationDispatch_ReturnsConfigurationForChaining()
    {
        var config = new EncinaConfiguration();

        var result = config.UseParallelNotificationDispatch();

        result.ShouldBe(config);
    }

    [Fact]
    public void RegisterServicesFromAssembly_AddsAssembly()
    {
        var config = new EncinaConfiguration();
        var assembly = Assembly.GetExecutingAssembly();

        config.RegisterServicesFromAssembly(assembly);

        // Access internal Assemblies through reflection for testing
        var assemblies = GetAssemblies(config);
        assemblies.ShouldContain(assembly);
    }

    [Fact]
    public void RegisterServicesFromAssembly_WithNullAssembly_ThrowsArgumentNullException()
    {
        var config = new EncinaConfiguration();

        Should.Throw<ArgumentNullException>(() => config.RegisterServicesFromAssembly(null!));
    }

    [Fact]
    public void RegisterServicesFromAssemblies_AddsMultipleAssemblies()
    {
        var config = new EncinaConfiguration();
        var assembly1 = Assembly.GetExecutingAssembly();
        var assembly2 = typeof(string).Assembly;

        config.RegisterServicesFromAssemblies(assembly1, assembly2);

        var assemblies = GetAssemblies(config);
        assemblies.Count.ShouldBe(2);
    }

    [Fact]
    public void RegisterServicesFromAssemblies_WithNullArray_DoesNotThrow()
    {
        var config = new EncinaConfiguration();

        Should.NotThrow(() => config.RegisterServicesFromAssemblies(null!));
    }

    [Fact]
    public void RegisterServicesFromAssemblyContaining_AddsContainingAssembly()
    {
        var config = new EncinaConfiguration();

        config.RegisterServicesFromAssemblyContaining<EncinaConfigurationTests>();

        var assemblies = GetAssemblies(config);
        assemblies.ShouldContain(typeof(EncinaConfigurationTests).Assembly);
    }

    [Fact]
    public void RegisterServicesFromAssembly_DoesNotAddDuplicates()
    {
        var config = new EncinaConfiguration();
        var assembly = Assembly.GetExecutingAssembly();

        config.RegisterServicesFromAssembly(assembly);
        config.RegisterServicesFromAssembly(assembly);

        var assemblies = GetAssemblies(config);
        assemblies.Count.ShouldBe(1);
    }

    private static IReadOnlyCollection<Assembly> GetAssemblies(EncinaConfiguration config)
    {
        // Use reflection to access internal Assemblies property
        var property = typeof(EncinaConfiguration).GetProperty("Assemblies", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IReadOnlyCollection<Assembly>)property!.GetValue(config)!;
    }
}
