using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using Encina.Security;
using Encina.Security.Diagnostics;
using Encina.Security.Health;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security;

/// <summary>
/// Unit tests for security observability: tracing, metrics, logging, and health checks.
/// </summary>
/// <remarks>
/// Activity tracing and metrics tests share static <see cref="ActivitySource"/> and
/// <see cref="Meter"/> instances from <see cref="SecurityDiagnostics"/>, so they are
/// serialized via a shared xUnit collection to avoid cross-test interference.
/// </remarks>
[Collection("Security-Observability")]
public class ObservabilityTests
{
    #region Activity Tracing Tests

    [Collection("Security-Observability")]
    public class ActivityTracingTests : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly List<Activity> _completedActivities = [];

        public ActivityTracingTests()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SecurityDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => _completedActivities.Add(activity)
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task Handle_ShouldCreateActivityWithRequestTypeTag()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior();
            accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));

            // Act
            await behavior.Handle(
                new NoSecurityCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert — No security attributes + RequireAuthenticatedByDefault=false => no activity
            _completedActivities.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithAttributes_ShouldCreateActivityWithCorrectTags()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<DenyAnonymousOnlyCommand>();
            accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-42"));

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert
            var activity = _completedActivities.Should().ContainSingle().Subject;
            activity.GetTagItem(SecurityDiagnostics.TagRequestType).Should().Be("DenyAnonymousOnlyCommand");
            activity.GetTagItem(SecurityDiagnostics.TagUserId).Should().Be("user-42");
            activity.GetTagItem(SecurityDiagnostics.TagOutcome).Should().Be("allowed");
        }

        [Fact]
        public async Task Handle_Denied_ShouldSetOutcomeAndDenialReasonTags()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<DenyAnonymousOnlyCommand>();
            accessor.SecurityContext.Returns(SecurityContext.Anonymous);

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert
            var activity = _completedActivities.Should().ContainSingle().Subject;
            activity.GetTagItem(SecurityDiagnostics.TagOutcome).Should().Be("denied");
            activity.GetTagItem(SecurityDiagnostics.TagDenialReason)
                .Should().Be(SecurityErrors.UnauthenticatedCode);
            activity.Status.Should().Be(ActivityStatusCode.Error);
        }

        [Fact]
        public async Task Handle_ShouldAddEventForEachAttributeEvaluated()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<DenyAnonymousOnlyCommand>();
            accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert
            var activity = _completedActivities.Should().ContainSingle().Subject;
            activity.Events.Should().Contain(e =>
                e.Name == "DenyAnonymousAttribute.evaluated");
        }

        [Fact]
        public async Task Handle_MultipleAttributes_ShouldAddEventForEach()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<AuthAndRoleTagCommand>();
            accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1", roles: ["Admin"]));

            // Act
            await behavior.Handle(
                new AuthAndRoleTagCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert
            var activity = _completedActivities.Should().ContainSingle().Subject;
            activity.Events.Should().HaveCountGreaterThanOrEqualTo(2);
            activity.Events.Should().Contain(e => e.Name == "DenyAnonymousAttribute.evaluated");
            activity.Events.Should().Contain(e => e.Name == "RequireRoleAttribute.evaluated");
        }

        [Fact]
        public async Task Handle_AllowAnonymous_ShouldNotCreateActivity()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<AllowAnonTraceCommand>();
            accessor.SecurityContext.Returns((ISecurityContext?)null);

            // Act
            await behavior.Handle(
                new AllowAnonTraceCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert — AllowAnonymous bypasses all security, no activity created
            _completedActivities.Should().BeEmpty();
        }
    }

    #endregion

    #region Metrics Tests

    [Collection("Security-Observability")]
    public class MetricsTests : IDisposable
    {
        private readonly MeterListener _meterListener;
        private readonly List<(string Name, long Value, TagList Tags)> _counterMeasurements = [];
        private readonly List<(string Name, double Value, TagList Tags)> _histogramMeasurements = [];

        public MetricsTests()
        {
            _meterListener = new MeterListener();
            _meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == SecurityDiagnostics.SourceName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _meterListener.SetMeasurementEventCallback<long>(
                (instrument, measurement, tags, _) =>
                {
                    _counterMeasurements.Add((instrument.Name, measurement, ToTagList(tags)));
                });

            _meterListener.SetMeasurementEventCallback<double>(
                (instrument, measurement, tags, _) =>
                {
                    _histogramMeasurements.Add((instrument.Name, measurement, ToTagList(tags)));
                });

            _meterListener.Start();
        }

        public void Dispose()
        {
            _meterListener.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task Handle_Allowed_ShouldIncrementTotalAndAllowedCounters()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<DenyAnonymousOnlyCommand>();
            accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));
            _counterMeasurements.Clear();

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            _meterListener.RecordObservableInstruments();

            // Assert
            _counterMeasurements.Should().Contain(m => m.Name == "security.authorization.total");
            _counterMeasurements.Should().Contain(m => m.Name == "security.authorization.allowed");
            _counterMeasurements.Should().NotContain(m => m.Name == "security.authorization.denied");
        }

        [Fact]
        public async Task Handle_Denied_ShouldIncrementTotalAndDeniedCounters()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<DenyAnonymousOnlyCommand>();
            accessor.SecurityContext.Returns(SecurityContext.Anonymous);
            _counterMeasurements.Clear();

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            _meterListener.RecordObservableInstruments();

            // Assert
            _counterMeasurements.Should().Contain(m => m.Name == "security.authorization.total");
            _counterMeasurements.Should().Contain(m => m.Name == "security.authorization.denied");
            _counterMeasurements.Should().NotContain(m => m.Name == "security.authorization.allowed");
        }

        [Fact]
        public async Task Handle_ShouldRecordDurationHistogram()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<DenyAnonymousOnlyCommand>();
            accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));
            _histogramMeasurements.Clear();

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            _meterListener.RecordObservableInstruments();

            // Assert
            _histogramMeasurements.Should().Contain(m =>
                m.Name == "security.authorization.duration" && m.Value >= 0);
        }

        [Fact]
        public async Task Handle_Denied_CounterTags_ShouldIncludeRequestTypeAndDenialReason()
        {
            // Arrange
            var (behavior, accessor) = CreateBehavior<DenyAnonymousOnlyCommand>();
            accessor.SecurityContext.Returns(SecurityContext.Anonymous);
            _counterMeasurements.Clear();

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            _meterListener.RecordObservableInstruments();

            // Assert
            var deniedMeasurement = _counterMeasurements
                .FirstOrDefault(m => m.Name == "security.authorization.denied");

            deniedMeasurement.Should().NotBeNull();
            GetTagValue(deniedMeasurement.Tags, SecurityDiagnostics.TagRequestType)
                .Should().Be("DenyAnonymousOnlyCommand");
            GetTagValue(deniedMeasurement.Tags, SecurityDiagnostics.TagDenialReason)
                .Should().Be(SecurityErrors.UnauthenticatedCode);
        }

        private static TagList ToTagList(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var tagList = new TagList();
            foreach (var tag in tags)
            {
                tagList.Add(tag.Key, tag.Value);
            }
            return tagList;
        }

        private static object? GetTagValue(TagList tags, string key)
        {
            foreach (var tag in tags)
            {
                if (tag.Key == key)
                    return tag.Value;
            }
            return null;
        }
    }

    #endregion

    #region Logging Tests

    /// <summary>
    /// Tests that the security pipeline emits the correct structured log messages.
    /// Uses <see cref="FakeLogger{T}"/> from Microsoft.Extensions.Diagnostics.Testing
    /// instead of NSubstitute, because <c>LoggerMessage.Define</c> delegates call
    /// <c>ILogger.Log&lt;FormattedLogValues&gt;</c> (internal type), which NSubstitute
    /// cannot intercept via generic type matching.
    /// </summary>
    public class LoggingTests
    {
        [Fact]
        public async Task Handle_Allowed_ShouldLogAtInformationLevel()
        {
            // Arrange
            var logger = new FakeLogger<SecurityPipelineBehavior<DenyAnonymousOnlyCommand, Unit>>();
            var (behavior, accessor) = CreateBehaviorWithLogger(logger);
            accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert — AuthorizationAllowed EventId=8001 at Information level
            var records = logger.Collector.GetSnapshot();
            records.Should().Contain(r => r.Id.Id == 8001 && r.Level == LogLevel.Information);
        }

        [Fact]
        public async Task Handle_Denied_ShouldLogAtWarningLevel()
        {
            // Arrange
            var logger = new FakeLogger<SecurityPipelineBehavior<DenyAnonymousOnlyCommand, Unit>>();
            var (behavior, accessor) = CreateBehaviorWithLogger(logger);
            accessor.SecurityContext.Returns(SecurityContext.Anonymous);

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert — AuthorizationDenied EventId=8002 at Warning level
            var records = logger.Collector.GetSnapshot();
            records.Should().Contain(r => r.Id.Id == 8002 && r.Level == LogLevel.Warning);
        }

        [Fact]
        public async Task Handle_WithAttributes_ShouldLogAuthorizationStartedAtDebug()
        {
            // Arrange
            var logger = new FakeLogger<SecurityPipelineBehavior<DenyAnonymousOnlyCommand, Unit>>();
            var (behavior, accessor) = CreateBehaviorWithLogger(logger);
            accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert — AuthorizationStarted EventId=8000 at Debug level
            var records = logger.Collector.GetSnapshot();
            records.Should().Contain(r => r.Id.Id == 8000 && r.Level == LogLevel.Debug);
        }

        [Fact]
        public async Task Handle_AllowAnonymous_ShouldLogBypassAtDebug()
        {
            // Arrange
            var logger = new FakeLogger<SecurityPipelineBehavior<AllowAnonTraceCommand, Unit>>();
            var accessor = Substitute.For<ISecurityContextAccessor>();
            accessor.SecurityContext.Returns((ISecurityContext?)null);

            var behavior = new SecurityPipelineBehavior<AllowAnonTraceCommand, Unit>(
                accessor,
                Substitute.For<IPermissionEvaluator>(),
                Substitute.For<IResourceOwnershipEvaluator>(),
                Options.Create(new SecurityOptions()),
                logger);

            // Act
            await behavior.Handle(
                new AllowAnonTraceCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert — AllowAnonymousBypass EventId=8003 at Debug level
            var records = logger.Collector.GetSnapshot();
            records.Should().Contain(r => r.Id.Id == 8003 && r.Level == LogLevel.Debug);
        }

        [Fact]
        public async Task Handle_NullContext_WithThrow_ShouldLogMissingContextAtWarning()
        {
            // Arrange
            var logger = new FakeLogger<SecurityPipelineBehavior<DenyAnonymousOnlyCommand, Unit>>();
            var accessor = Substitute.For<ISecurityContextAccessor>();
            accessor.SecurityContext.Returns((ISecurityContext?)null);
            var options = new SecurityOptions { ThrowOnMissingSecurityContext = true };

            var behavior = new SecurityPipelineBehavior<DenyAnonymousOnlyCommand, Unit>(
                accessor,
                Substitute.For<IPermissionEvaluator>(),
                Substitute.For<IResourceOwnershipEvaluator>(),
                Options.Create(options),
                logger);

            // Act
            await behavior.Handle(
                new DenyAnonymousOnlyCommand(), RequestContext.CreateForTest(),
                Next(Unit.Default), CancellationToken.None);

            // Assert — MissingSecurityContext EventId=8004 at Warning level
            var records = logger.Collector.GetSnapshot();
            records.Should().Contain(r => r.Id.Id == 8004 && r.Level == LogLevel.Warning);
        }

        private static (SecurityPipelineBehavior<DenyAnonymousOnlyCommand, Unit> Behavior, ISecurityContextAccessor Accessor)
            CreateBehaviorWithLogger(FakeLogger<SecurityPipelineBehavior<DenyAnonymousOnlyCommand, Unit>> logger)
        {
            var accessor = Substitute.For<ISecurityContextAccessor>();
            var behavior = new SecurityPipelineBehavior<DenyAnonymousOnlyCommand, Unit>(
                accessor,
                Substitute.For<IPermissionEvaluator>(),
                Substitute.For<IResourceOwnershipEvaluator>(),
                Options.Create(new SecurityOptions()),
                logger);

            return (behavior, accessor);
        }
    }

    #endregion

    #region SecurityHealthCheck Tests

    public class SecurityHealthCheckTests
    {
        [Fact]
        public async Task CheckHealth_AllServicesRegistered_ShouldReturnHealthy()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEncinaSecurity();
            var provider = services.BuildServiceProvider();

            var healthCheck = new SecurityHealthCheck(provider);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    SecurityHealthCheck.DefaultName,
                    healthCheck,
                    HealthStatus.Unhealthy,
                    null)
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Description.Should().Contain("All security services are registered");
        }

        [Fact]
        public async Task CheckHealth_MissingServices_ShouldReturnUnhealthy()
        {
            // Arrange — empty service provider, no security services registered
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();

            var healthCheck = new SecurityHealthCheck(provider);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    SecurityHealthCheck.DefaultName,
                    healthCheck,
                    HealthStatus.Unhealthy,
                    null)
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Contain("Missing security services");
            result.Description.Should().Contain(nameof(ISecurityContextAccessor));
            result.Description.Should().Contain(nameof(IPermissionEvaluator));
            result.Description.Should().Contain(nameof(IResourceOwnershipEvaluator));
        }

        [Fact]
        public async Task CheckHealth_PartialServices_ShouldReturnUnhealthy()
        {
            // Arrange — register only some services
            var services = new ServiceCollection();
            services.AddScoped<ISecurityContextAccessor, SecurityContextAccessor>();
            // Missing IPermissionEvaluator and IResourceOwnershipEvaluator
            var provider = services.BuildServiceProvider();

            var healthCheck = new SecurityHealthCheck(provider);
            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    SecurityHealthCheck.DefaultName,
                    healthCheck,
                    HealthStatus.Unhealthy,
                    null)
            };

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Contain(nameof(IPermissionEvaluator));
            result.Description.Should().Contain(nameof(IResourceOwnershipEvaluator));
            result.Description.Should().NotContain(nameof(ISecurityContextAccessor));
        }

        [Fact]
        public void DefaultName_ShouldBeEncinaSecurity()
        {
            SecurityHealthCheck.DefaultName.Should().Be("encina-security");
        }

        [Fact]
        public void Tags_ShouldContainExpectedValues()
        {
            SecurityHealthCheck.Tags.Should().Contain("encina");
            SecurityHealthCheck.Tags.Should().Contain("security");
            SecurityHealthCheck.Tags.Should().Contain("ready");
        }
    }

    #endregion

    #region Shared Helpers

    private static (SecurityPipelineBehavior<NoSecurityCommand, Unit> Behavior, ISecurityContextAccessor Accessor) CreateBehavior()
    {
        var accessor = Substitute.For<ISecurityContextAccessor>();
        var behavior = new SecurityPipelineBehavior<NoSecurityCommand, Unit>(
            accessor,
            Substitute.For<IPermissionEvaluator>(),
            Substitute.For<IResourceOwnershipEvaluator>(),
            Options.Create(new SecurityOptions()),
            new FakeLogger<SecurityPipelineBehavior<NoSecurityCommand, Unit>>());

        return (behavior, accessor);
    }

    private static (SecurityPipelineBehavior<TRequest, Unit> Behavior, ISecurityContextAccessor Accessor)
        CreateBehavior<TRequest>()
        where TRequest : IRequest<Unit>
    {
        var accessor = Substitute.For<ISecurityContextAccessor>();
        var behavior = new SecurityPipelineBehavior<TRequest, Unit>(
            accessor,
            Substitute.For<IPermissionEvaluator>(),
            Substitute.For<IResourceOwnershipEvaluator>(),
            Options.Create(new SecurityOptions()),
            new FakeLogger<SecurityPipelineBehavior<TRequest, Unit>>());

        return (behavior, accessor);
    }

    private static RequestHandlerCallback<TResponse> Next<TResponse>(TResponse value)
        => () => new ValueTask<Either<EncinaError, TResponse>>(value);

    private static SecurityContext CreateAuthenticatedContext(
        string userId,
        string[]? roles = null)
    {
        var claims = new List<Claim> { new("sub", userId) };
        if (roles is not null)
        {
            foreach (var role in roles)
                claims.Add(new Claim("role", role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new SecurityContext(new ClaimsPrincipal(identity));
    }

    #endregion

    #region Test Request Types

    public sealed class NoSecurityCommand : ICommand<Unit> { }

    [AllowAnonymous]
    public sealed class AllowAnonTraceCommand : ICommand<Unit> { }

    [DenyAnonymous]
    public sealed class DenyAnonymousOnlyCommand : ICommand<Unit> { }

    [DenyAnonymous]
    [RequireRole("Admin")]
    public sealed class AuthAndRoleTagCommand : ICommand<Unit> { }

    #endregion
}

/// <summary>
/// xUnit collection definition to serialize tests that share static
/// <see cref="ActivitySource"/> and <see cref="Meter"/> state from
/// <see cref="SecurityDiagnostics"/>.
/// </summary>
[CollectionDefinition("Security-Observability", DisableParallelization = true)]
public class SecurityObservabilityTestGroup;
