using Encina.gRPC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.gRPC.Tests;

/// <summary>
/// Tests for the <see cref="ServiceCollectionExtensions"/> class.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddEncinaGrpc_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IEncina>(_ => NSubstitute.Substitute.For<IEncina>());
        services.AddLogging();

        // Act
        services.AddEncinaGrpc();

        // Assert
        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var grpcService = scope.ServiceProvider.GetService<IGrpcEncinaService>();
        grpcService.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaGrpc_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IEncina>(_ => NSubstitute.Substitute.For<IEncina>());
        services.AddLogging();

        // Act
        services.AddEncinaGrpc(options =>
        {
            options.EnableReflection = false;
            options.EnableHealthChecks = false;
            options.MaxReceiveMessageSize = 8 * 1024 * 1024;
            options.MaxSendMessageSize = 8 * 1024 * 1024;
            options.EnableLoggingInterceptor = false;
            options.DefaultDeadline = TimeSpan.FromSeconds(60);
            options.EnableCompression = true;
        });

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<EncinaGrpcOptions>>().Value;
        options.EnableReflection.ShouldBeFalse();
        options.EnableHealthChecks.ShouldBeFalse();
        options.MaxReceiveMessageSize.ShouldBe(8 * 1024 * 1024);
        options.MaxSendMessageSize.ShouldBe(8 * 1024 * 1024);
        options.EnableLoggingInterceptor.ShouldBeFalse();
        options.DefaultDeadline.ShouldBe(TimeSpan.FromSeconds(60));
        options.EnableCompression.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaGrpc_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddEncinaGrpc());
    }

    [Fact]
    public void AddEncinaGrpc_WithNullConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IEncina>(_ => NSubstitute.Substitute.For<IEncina>());
        services.AddLogging();

        // Act
        services.AddEncinaGrpc(null);

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<EncinaGrpcOptions>>().Value;
        options.EnableReflection.ShouldBeTrue();
        options.EnableHealthChecks.ShouldBeTrue();
        options.MaxReceiveMessageSize.ShouldBe(4 * 1024 * 1024);
        options.MaxSendMessageSize.ShouldBe(4 * 1024 * 1024);
        options.EnableLoggingInterceptor.ShouldBeTrue();
        options.DefaultDeadline.ShouldBe(TimeSpan.FromSeconds(30));
        options.EnableCompression.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaGrpc_CalledTwice_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IEncina>(_ => NSubstitute.Substitute.For<IEncina>());
        services.AddLogging();

        // Act
        services.AddEncinaGrpc();
        services.AddEncinaGrpc();

        // Assert
        var grpcServiceDescriptors = services.Where(d => d.ServiceType == typeof(IGrpcEncinaService)).ToList();
        grpcServiceDescriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaGrpc_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IEncina>(_ => NSubstitute.Substitute.For<IEncina>());
        services.AddLogging();

        // Act
        var result = services.AddEncinaGrpc();

        // Assert
        result.ShouldBeSameAs(services);
    }
}
