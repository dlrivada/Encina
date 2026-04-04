using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionFluentPolicyHostedService"/> constructor and lifecycle.
/// </summary>
public sealed class RetentionFluentPolicyHostedServiceGuardTests
{
    private readonly RetentionFluentPolicyDescriptor _descriptor = new([]);
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    private readonly ILogger<RetentionFluentPolicyHostedService> _logger =
        NullLogger<RetentionFluentPolicyHostedService>.Instance;

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var act = () => new RetentionFluentPolicyHostedService(
            _descriptor, _scopeFactory, _logger);

        Should.NotThrow(act);
    }

    [Fact]
    public async Task StartAsync_EmptyPolicies_CompletesImmediately()
    {
        var sut = new RetentionFluentPolicyHostedService(
            _descriptor, _scopeFactory, _logger);

        await sut.StartAsync(CancellationToken.None);

        // Should complete without creating scope
        _scopeFactory.DidNotReceive().CreateAsyncScope();
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        var sut = new RetentionFluentPolicyHostedService(
            _descriptor, _scopeFactory, _logger);

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Constructor_NullDescriptor_CreatesInstanceButMayFailAtRuntime()
    {
        // The constructor does not guard against null descriptor;
        // it would fail at StartAsync time
        var act = () => new RetentionFluentPolicyHostedService(
            null!, _scopeFactory, _logger);

        // This verifies the constructor parameter is stored, not validated
        Should.NotThrow(act);
    }
}
