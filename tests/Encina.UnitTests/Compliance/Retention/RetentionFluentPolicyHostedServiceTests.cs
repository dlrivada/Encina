using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionFluentPolicyHostedService"/>.
/// </summary>
public sealed class RetentionFluentPolicyHostedServiceTests
{
    private readonly IRetentionPolicyService _policyService = Substitute.For<IRetentionPolicyService>();

    private IServiceScopeFactory CreateScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_policyService);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    #region StartAsync - Empty Policies

    [Fact]
    public async Task StartAsync_EmptyPolicies_DoesNotCreateScope()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var descriptor = new RetentionFluentPolicyDescriptor([]);
        var sut = new RetentionFluentPolicyHostedService(
            descriptor, scopeFactory, NullLogger<RetentionFluentPolicyHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        scopeFactory.DidNotReceive().CreateAsyncScope();
    }

    #endregion

    #region StartAsync - Policy Creation

    [Fact]
    public async Task StartAsync_SinglePolicy_CreatesPolicy()
    {
        var descriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("test-cat", TimeSpan.FromDays(365), true, "Test reason", "GDPR Art 5")
        ]);

        _policyService.GetPolicyByCategoryAsync("test-cat", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("test-cat")));

        _policyService.CreatePolicyAsync(
                "test-cat", TimeSpan.FromDays(365), true,
                RetentionPolicyType.TimeBased,
                "Test reason", "GDPR Art 5",
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var sut = new RetentionFluentPolicyHostedService(
            descriptor, CreateScopeFactory(), NullLogger<RetentionFluentPolicyHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await _policyService.Received(1).CreatePolicyAsync(
            "test-cat", TimeSpan.FromDays(365), true,
            RetentionPolicyType.TimeBased,
            "Test reason", "GDPR Art 5",
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_PolicyAlreadyExists_SkipsCreation()
    {
        var descriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("existing-cat", TimeSpan.FromDays(90), true, "Reason", null)
        ]);

        _policyService.GetPolicyByCategoryAsync("existing-cat", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionPolicyReadModel>(new RetentionPolicyReadModel
            {
                Id = Guid.NewGuid(),
                DataCategory = "existing-cat",
                RetentionPeriod = TimeSpan.FromDays(90),
                IsActive = true
            }));

        var sut = new RetentionFluentPolicyHostedService(
            descriptor, CreateScopeFactory(), NullLogger<RetentionFluentPolicyHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await _policyService.DidNotReceive().CreatePolicyAsync(
            Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
            Arg.Any<RetentionPolicyType>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_CreatePolicyReturnsError_LogsWarning()
    {
        var descriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("fail-cat", TimeSpan.FromDays(30), true, "Reason", null)
        ]);

        _policyService.GetPolicyByCategoryAsync("fail-cat", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("fail-cat")));

        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Guid>(
                RetentionErrors.StoreError("Create", "Connection refused")));

        var sut = new RetentionFluentPolicyHostedService(
            descriptor, CreateScopeFactory(), NullLogger<RetentionFluentPolicyHostedService>.Instance);

        // Should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_CreatePolicyThrowsException_LogsWarningAndContinues()
    {
        var descriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("throw-cat", TimeSpan.FromDays(30), true, "Reason", null)
        ]);

        _policyService.GetPolicyByCategoryAsync("throw-cat", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("throw-cat")));

#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Guid>>>(_ =>
                throw new InvalidOperationException("Unexpected error"));
#pragma warning restore CA2012

        var sut = new RetentionFluentPolicyHostedService(
            descriptor, CreateScopeFactory(), NullLogger<RetentionFluentPolicyHostedService>.Instance);

        // Should not throw
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_MultiplePolicies_CreatesAllNewOnes()
    {
        var descriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("cat-1", TimeSpan.FromDays(30), true, "R1", null),
            new RetentionPolicyDescriptor("cat-2", TimeSpan.FromDays(60), false, "R2", "Legal"),
            new RetentionPolicyDescriptor("cat-3", TimeSpan.FromDays(90), true, null, null)
        ]);

        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("any")));

        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var sut = new RetentionFluentPolicyHostedService(
            descriptor, CreateScopeFactory(), NullLogger<RetentionFluentPolicyHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await _policyService.Received(3).CreatePolicyAsync(
            Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
            Arg.Any<RetentionPolicyType>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_NullReasonPolicy_UsesDefaultReason()
    {
        var descriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("null-reason", TimeSpan.FromDays(30), true, null, null)
        ]);

        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("null-reason")));

        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var sut = new RetentionFluentPolicyHostedService(
            descriptor, CreateScopeFactory(), NullLogger<RetentionFluentPolicyHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await _policyService.Received(1).CreatePolicyAsync(
            "null-reason", TimeSpan.FromDays(30), true,
            RetentionPolicyType.TimeBased,
            "Configured via AddPolicy() fluent API",
            Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region StopAsync

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        var descriptor = new RetentionFluentPolicyDescriptor([]);
        var sut = new RetentionFluentPolicyHostedService(
            descriptor, CreateScopeFactory(), NullLogger<RetentionFluentPolicyHostedService>.Instance);

        var task = sut.StopAsync(CancellationToken.None);

        task.IsCompletedSuccessfully.ShouldBeTrue();
    }

    #endregion
}
