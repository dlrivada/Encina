using System.Reflection;

using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionAutoRegistrationHostedService"/>.
/// </summary>
public sealed class RetentionAutoRegistrationHostedServiceTests
{
    private readonly IRetentionPolicyService _policyService = Substitute.For<IRetentionPolicyService>();

    private IServiceScopeFactory CreateScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_policyService);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    #region StartAsync - Skip Scenarios

    [Fact]
    public async Task StartAsync_AutoRegisterDisabledAndNoFluent_SkipsRegistration()
    {
        var options = new RetentionOptions { AutoRegisterFromAttributes = false };
        var descriptor = new RetentionAutoRegistrationDescriptor([]);
        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await _policyService.DidNotReceive()
            .CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_AutoRegisterEnabledButEmptyAssembliesNoFluent_SkipsRegistration()
    {
        var options = new RetentionOptions { AutoRegisterFromAttributes = true };
        var descriptor = new RetentionAutoRegistrationDescriptor([]);
        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await _policyService.DidNotReceive()
            .CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
    }

    #endregion

    #region StartAsync - Attribute Discovery

    [Fact]
    public async Task StartAsync_WithAttributeDecoratedTypes_DiscoversPolicies()
    {
        // Scan the test assembly which contains our test types below
        var testAssembly = typeof(RetentionAutoRegistrationHostedServiceTests).Assembly;
        var options = new RetentionOptions { AutoRegisterFromAttributes = true };
        var descriptor = new RetentionAutoRegistrationDescriptor([testAssembly]);

        // Setup policy service to return "not found" for get, "success" for create
        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("test")));

        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        // Should have attempted to create at least some policies
        // (test assembly contains decorated types below)
        await _policyService.Received().CreatePolicyAsync(
            Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
            Arg.Any<RetentionPolicyType>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_PolicyAlreadyExists_SkipsCreation()
    {
        var testAssembly = typeof(RetentionAutoRegistrationHostedServiceTests).Assembly;
        var options = new RetentionOptions { AutoRegisterFromAttributes = true };
        var descriptor = new RetentionAutoRegistrationDescriptor([testAssembly]);

        // All policies already exist
        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionPolicyReadModel>(new RetentionPolicyReadModel
            {
                Id = Guid.NewGuid(),
                DataCategory = "test-data",
                RetentionPeriod = TimeSpan.FromDays(365),
                IsActive = true
            }));

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        // Should NOT create any policies since they all exist
        await _policyService.DidNotReceive()
            .CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_CreatePolicyFails_LogsWarningAndContinues()
    {
        var testAssembly = typeof(RetentionAutoRegistrationHostedServiceTests).Assembly;
        var options = new RetentionOptions { AutoRegisterFromAttributes = true };
        var descriptor = new RetentionAutoRegistrationDescriptor([testAssembly]);

        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("test")));

        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Guid>(
                RetentionErrors.StoreError("Create", "Store unavailable")));

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance);

        // Should not throw, should handle errors gracefully
        await sut.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_CreatePolicyThrows_LogsWarningAndContinues()
    {
        var testAssembly = typeof(RetentionAutoRegistrationHostedServiceTests).Assembly;
        var options = new RetentionOptions { AutoRegisterFromAttributes = true };
        var descriptor = new RetentionAutoRegistrationDescriptor([testAssembly]);

        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("test")));

#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Guid>>>(_ =>
                throw new InvalidOperationException("Database unavailable"));
#pragma warning restore CA2012

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance);

        // Should not throw — exception is caught and logged
        await sut.StartAsync(CancellationToken.None);
    }

    #endregion

    #region StartAsync - Fluent Policy Merge

    [Fact]
    public async Task StartAsync_WithFluentPoliciesOnly_CreatesFluentPolicies()
    {
        var options = new RetentionOptions { AutoRegisterFromAttributes = false };
        var descriptor = new RetentionAutoRegistrationDescriptor([]);
        var fluentDescriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("fluent-category", TimeSpan.FromDays(180), true, "Fluent reason", null)
        ]);

        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("test")));

        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance,
            fluentDescriptor);

        await sut.StartAsync(CancellationToken.None);

        await _policyService.Received(1).CreatePolicyAsync(
            "fluent-category",
            TimeSpan.FromDays(180),
            true,
            RetentionPolicyType.TimeBased,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_FluentPolicyWithLegalBasisButNoReason_UsesLegalBasisAsReason()
    {
        var options = new RetentionOptions { AutoRegisterFromAttributes = false };
        var descriptor = new RetentionAutoRegistrationDescriptor([]);
        var fluentDescriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("legal-category", TimeSpan.FromDays(365), false, null, "GDPR Art. 5")
        ]);

        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("test")));

        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance,
            fluentDescriptor);

        await sut.StartAsync(CancellationToken.None);

        // The reason should fall back to legal basis
        await _policyService.Received(1).CreatePolicyAsync(
            "legal-category",
            TimeSpan.FromDays(365),
            false,
            RetentionPolicyType.TimeBased,
            "GDPR Art. 5",
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_FluentPolicyWithNeitherReasonNorLegalBasis_UsesDefaultReason()
    {
        var options = new RetentionOptions { AutoRegisterFromAttributes = false };
        var descriptor = new RetentionAutoRegistrationDescriptor([]);
        var fluentDescriptor = new RetentionFluentPolicyDescriptor([
            new RetentionPolicyDescriptor("no-reason", TimeSpan.FromDays(30), true, null, null)
        ]);

        _policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(
                RetentionErrors.PolicyNotFound("test")));

        _policyService.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Guid>(Guid.NewGuid()));

        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(options),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance,
            fluentDescriptor);

        await sut.StartAsync(CancellationToken.None);

        await _policyService.Received(1).CreatePolicyAsync(
            "no-reason",
            TimeSpan.FromDays(30),
            true,
            RetentionPolicyType.TimeBased,
            "Configured via AddPolicy() fluent API",
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region StopAsync

    [Fact]
    public async Task StopAsync_CompletesImmediately()
    {
        var sut = new RetentionAutoRegistrationHostedService(
            new RetentionAutoRegistrationDescriptor([]),
            Options.Create(new RetentionOptions()),
            CreateScopeFactory(),
            NullLogger<RetentionAutoRegistrationHostedService>.Instance);

        var task = sut.StopAsync(CancellationToken.None);

        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    #endregion

    // ========================================================================
    // Test types with [RetentionPeriod] attributes for discovery testing
    // ========================================================================

    [RetentionPeriod(Years = 7, DataCategory = "auto-reg-test-financial",
        Reason = "Tax law retention")]
    internal sealed record AutoRegTestFinancialRecord
    {
        public string Id { get; init; } = string.Empty;
    }

    internal sealed record AutoRegTestRecordWithPropertyAttribute
    {
        public string Id { get; init; } = string.Empty;

        [RetentionPeriod(Days = 90, DataCategory = "auto-reg-test-session-logs")]
        public string? SessionData { get; init; }
    }
}
