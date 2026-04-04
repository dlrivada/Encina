using System.Reflection;

using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionAutoRegistrationHostedService"/> constructor and StartAsync parameter handling.
/// </summary>
public sealed class RetentionAutoRegistrationHostedServiceGuardTests
{
    private readonly RetentionAutoRegistrationDescriptor _descriptor =
        new(Array.Empty<Assembly>());

    private readonly IOptions<RetentionOptions> _options = Options.Create(new RetentionOptions());
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    private readonly ILogger<RetentionAutoRegistrationHostedService> _logger =
        NullLogger<RetentionAutoRegistrationHostedService>.Instance;

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var act = () => new RetentionAutoRegistrationHostedService(
            _descriptor, _options, _scopeFactory, _logger);

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_ValidParametersWithFluentDescriptor_DoesNotThrow()
    {
        var fluentDescriptor = new RetentionFluentPolicyDescriptor([]);

        var act = () => new RetentionAutoRegistrationHostedService(
            _descriptor, _options, _scopeFactory, _logger, fluentDescriptor);

        Should.NotThrow(act);
    }

    [Fact]
    public async Task StartAsync_NoBothWork_ReturnsImmediately()
    {
        var options = new RetentionOptions { AutoRegisterFromAttributes = false };
        var sut = new RetentionAutoRegistrationHostedService(
            _descriptor,
            Options.Create(options),
            _scopeFactory,
            _logger);

        // Should not throw when nothing to register
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        var sut = new RetentionAutoRegistrationHostedService(
            _descriptor, _options, _scopeFactory, _logger);

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_EmptyAssembliesAndNoFluentPolicies_CompletesGracefully()
    {
        var options = new RetentionOptions { AutoRegisterFromAttributes = true };
        var descriptor = new RetentionAutoRegistrationDescriptor([]);

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            _scopeFactory,
            _logger);

        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_AttributesDisabledNoFluentPolicies_SkipsRegistration()
    {
        var options = new RetentionOptions { AutoRegisterFromAttributes = false };
        var descriptor = new RetentionAutoRegistrationDescriptor([typeof(RetentionOptions).Assembly]);

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            _scopeFactory,
            _logger);

        // No fluent descriptor => should skip
        await sut.StartAsync(CancellationToken.None);
    }
}
