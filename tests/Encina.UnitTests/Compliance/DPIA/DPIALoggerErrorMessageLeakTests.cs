#pragma warning disable CA2012 // Use ValueTasks correctly

using System.Reflection;

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// <see cref="EncinaError.Message"/> must never reach a logger call in
/// <c>Encina.Compliance.DPIA</c>, directly or wrapped in a new exception created only to carry
/// the message into the log (#1503, extending #1499's health-check rule and #1454/#1488's
/// activity-tag rule to the logging sink). Only <c>error.GetCode().IfNone("encina.unknown")</c>
/// may reach the logger.
/// </summary>
public sealed class DPIALoggerErrorMessageLeakTests
{
    // A sentinel that must never appear in any log record. Chosen to be extremely unlikely to
    // collide with any error code or fixed outcome string used by the production code under test.
    private const string SentinelMessage = "SENTINEL-DPIA-LOG-MESSAGE-do-not-log-this-4f2c9e";

    private static readonly EncinaError SensitiveError =
        EncinaErrors.Create("dpia.store_error", SentinelMessage);

    #region DPIARequiredPipelineBehavior

    [Fact]
    public async Task Handle_BlockMode_StoreError_LogsOnlyTheErrorCode()
    {
        var options = Options.Create(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var logger = new FakeLogger<DPIARequiredPipelineBehavior<SensitiveCommand, string>>();
        var service = Substitute.For<IDPIAService>();
        service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DPIAReadModel>(SensitiveError));
        var sut = new DPIARequiredPipelineBehavior<SensitiveCommand, string>(
            service, options, new FakeTimeProvider(), logger);
        var context = Substitute.For<IRequestContext>();

        var result = await sut.Handle(
            new SensitiveCommand(),
            context,
            () => throw new InvalidOperationException("nextStep should not be called"),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dpia.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task Handle_WarnMode_StoreError_LogsOnlyTheErrorCode()
    {
        var options = Options.Create(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Warn });
        var logger = new FakeLogger<DPIARequiredPipelineBehavior<SensitiveCommand, string>>();
        var service = Substitute.For<IDPIAService>();
        service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DPIAReadModel>(SensitiveError));
        var sut = new DPIARequiredPipelineBehavior<SensitiveCommand, string>(
            service, options, new FakeTimeProvider(), logger);
        var context = Substitute.For<IRequestContext>();

        var result = await sut.Handle(
            new SensitiveCommand(),
            context,
            () => ValueTask.FromResult<Either<EncinaError, string>>("handler-result"),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dpia.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [RequiresDPIA(ProcessingType = "AutomatedDecisionMaking", Reason = "Test")]
    public sealed record SensitiveCommand : IRequest<string>;

    #endregion

    #region DPIAAutoRegistrationHostedService

    [Fact]
    public async Task StartAsync_CreateAssessmentReturnsError_LogsOnlyTheErrorCode()
    {
        var logger = new FakeLogger<DPIAAutoRegistrationHostedService>();
        var service = Substitute.For<IDPIAService>();
        service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, DPIAReadModel>(
                DPIAErrors.AssessmentNotFoundByRequestType("unknown"))));
        service.CreateAssessmentAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(SensitiveError));

        var options = Options.Create(new DPIAOptions
        {
            AutoRegisterFromAttributes = true,
            AutoDetectHighRisk = false,
            DefaultReviewPeriod = TimeSpan.FromDays(365)
        });
        var descriptor = new DPIAAutoRegistrationDescriptor([typeof(TestCommandWithDPIA).Assembly]);
        var sut = new DPIAAutoRegistrationHostedService(service, options, descriptor, logger);

        await sut.StartAsync(CancellationToken.None);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dpia.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
        logs.ShouldAllBe(r => r.Exception == null || !r.Exception.Message.Contains(SentinelMessage));
    }

    [RequiresDPIA(ProcessingType = "Testing", Reason = "Unit test type")]
    private sealed class TestCommandWithDPIA;

    #endregion

    #region DPIAReviewReminderService

    [Fact]
    public async Task ExecuteAsync_StoreReturnsError_LogsOnlyTheErrorCode()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>(SensitiveError));

        var scopeFactory = CreateScopeFactory(service);
        var logger = new FakeLogger<DPIAReviewReminderService>();
        var sut = new DPIAReviewReminderService(scopeFactory, Options.Create(options), logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await sut.StartAsync(cts.Token);
        await WaitUntilAsync(
            () => logger.Collector.GetSnapshot().Any(r => r.Message.Contains("dpia.store_error")),
            TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dpia.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
        logs.ShouldAllBe(r => r.Exception == null || !r.Exception.Message.Contains(SentinelMessage));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, CancellationToken.None);
        }
    }

    private static IServiceScopeFactory CreateScopeFactory(IDPIAService service)
    {
        var services = new ServiceCollection();
        services.AddSingleton(service);
        services.AddSingleton(TimeProvider.System);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    #endregion
}
#pragma warning restore CA2012
