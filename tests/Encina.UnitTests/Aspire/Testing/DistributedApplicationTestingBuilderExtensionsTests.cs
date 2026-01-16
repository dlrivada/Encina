using Aspire.Hosting.Testing;
using Encina.Aspire.Testing;
using Encina.Testing.Fakes;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Aspire.Testing;

/// <summary>
/// Unit tests for <see cref="DistributedApplicationTestingBuilderExtensions"/>.
/// </summary>
public sealed class DistributedApplicationTestingBuilderExtensionsTests
{
    private static IDistributedApplicationTestingBuilder CreateBuilder(out ServiceCollection services)
    {
        services = new ServiceCollection();
        var builder = Substitute.For<IDistributedApplicationTestingBuilder>();
        builder.Services.Returns(services);
        return builder;
    }

    [Fact]
    public void WithEncinaTestSupport_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IDistributedApplicationTestingBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.WithEncinaTestSupport());
    }

    [Fact]
    public void WithEncinaTestSupport_RegistersOptions()
    {
        // Arrange
        var builder = CreateBuilder(out var services);

        // Act
        builder.WithEncinaTestSupport();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<EncinaTestSupportOptions>();
        options.ShouldNotBeNull();
    }

    [Fact]
    public void WithEncinaTestSupport_ConfiguresOptions()
    {
        // Arrange
        var builder = CreateBuilder(out var services);

        // Act
        builder.WithEncinaTestSupport(opts =>
        {
            opts.ClearOutboxBeforeTest = false;
            opts.DefaultWaitTimeout = TimeSpan.FromMinutes(5);
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<EncinaTestSupportOptions>();
        options.ClearOutboxBeforeTest.ShouldBeFalse();
        options.DefaultWaitTimeout.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void WithEncinaTestSupport_RegistersFakeStores()
    {
        // Arrange
        var builder = CreateBuilder(out var services);

        // Act
        builder.WithEncinaTestSupport();

        // Assert
        var provider = services.BuildServiceProvider();
        provider.GetService<FakeOutboxStore>().ShouldNotBeNull();
        provider.GetService<FakeInboxStore>().ShouldNotBeNull();
        provider.GetService<FakeSagaStore>().ShouldNotBeNull();
        provider.GetService<FakeScheduledMessageStore>().ShouldNotBeNull();
        provider.GetService<FakeDeadLetterStore>().ShouldNotBeNull();
    }

    [Fact]
    public void WithEncinaTestSupport_RegistersTestContext()
    {
        // Arrange
        var builder = CreateBuilder(out var services);

        // Act
        builder.WithEncinaTestSupport();

        // Assert
        var provider = services.BuildServiceProvider();
        var context = provider.GetService<EncinaTestContext>();
        context.ShouldNotBeNull();
    }

    [Fact]
    public void WithEncinaTestSupport_ReturnsBuilder()
    {
        // Arrange
        var builder = CreateBuilder(out _);

        // Act
        var result = builder.WithEncinaTestSupport();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void WithEncinaTestSupport_WithNullConfigure_UsesDefaults()
    {
        // Arrange
        var builder = CreateBuilder(out var services);

        // Act
        builder.WithEncinaTestSupport(null);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<EncinaTestSupportOptions>();
        options.ClearOutboxBeforeTest.ShouldBeTrue(); // Default value
    }
}
