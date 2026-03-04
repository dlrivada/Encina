#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection;
using Encina.Compliance.BreachNotification.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachDetectionPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class BreachDetectionPipelineBehaviorTests
{
    private readonly IBreachDetector _detector;
    private readonly IBreachHandler _handler;
    private readonly FakeTimeProvider _timeProvider;

    public BreachDetectionPipelineBehaviorTests()
    {
        _detector = Substitute.For<IBreachDetector>();
        _handler = Substitute.For<IBreachHandler>();
        _timeProvider = new FakeTimeProvider();

        // Default: detector returns no breaches
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                Right<EncinaError, IReadOnlyList<PotentialBreach>>(
                    (IReadOnlyList<PotentialBreach>)[])));

        // Default: handler returns Right(BreachRecord)
        _handler.HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, BreachRecord>>(
                Right<EncinaError, BreachRecord>(
                    BreachRecord.Create(
                        "Test breach", 100, ["email"], "dpo@test.com",
                        "Identity theft", "Access revoked",
                        DateTimeOffset.UtcNow, BreachSeverity.High))));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDetector_ShouldThrow()
    {
        var act = () => new BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>(
            null!, _handler, Options.Create(new BreachNotificationOptions()),
            _timeProvider,
            NullLogger<BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("detector");
    }

    [Fact]
    public void Constructor_NullHandler_ShouldThrow()
    {
        var act = () => new BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>(
            _detector, null!, Options.Create(new BreachNotificationOptions()),
            _timeProvider,
            NullLogger<BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var act = () => new BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>(
            _detector, _handler, null!, _timeProvider,
            NullLogger<BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>(
            _detector, _handler, Options.Create(new BreachNotificationOptions()),
            null!,
            NullLogger<BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>(
            _detector, _handler, Options.Create(new BreachNotificationOptions()),
            _timeProvider, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Enforcement Disabled

    [Fact]
    public async Task Handle_EnforcementDisabled_ShouldSkipDetection()
    {
        // Arrange
        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Disabled);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _detector.DidNotReceive()
            .DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EnforcementDisabled_ShouldNotCallHandler()
    {
        // Arrange
        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Disabled);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await _handler.DidNotReceive()
            .HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region No Attribute — Passthrough

    [Fact]
    public async Task Handle_NoBreachMonitoredAttribute_ShouldPassthrough()
    {
        // Arrange
        var behavior = CreateUnmonitoredBehavior();
        var request = new SampleUnmonitoredRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _detector.DidNotReceive()
            .DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoBreachMonitoredAttribute_ShouldNotCallHandler()
    {
        // Arrange
        var behavior = CreateUnmonitoredBehavior();
        var request = new SampleUnmonitoredRequest();

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await _handler.DidNotReceive()
            .HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region No Breach Detected

    [Fact]
    public async Task Handle_NoBreachDetected_ReturnsOriginalResult()
    {
        // Arrange — default detector returns empty breach list
        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _handler.DidNotReceive()
            .HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Breach Detected — Block Mode

    [Fact]
    public async Task Handle_BreachDetected_BlockMode_ReturnsError()
    {
        // Arrange
        var potentialBreach = CreatePotentialBreach("TestRule");
        SetupDetectorReturnsBreaches(potentialBreach);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().Match(
            Some: code => code.Should().Be(BreachNotificationErrors.BreachDetectedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task Handle_BreachDetected_BlockMode_ErrorContainsRuleName()
    {
        // Arrange
        var potentialBreach = CreatePotentialBreach("UnauthorizedAccessRule");
        SetupDetectorReturnsBreaches(potentialBreach);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("UnauthorizedAccessRule");
    }

    [Fact]
    public async Task Handle_BreachDetected_BlockMode_ErrorContainsRequestTypeName()
    {
        // Arrange
        var potentialBreach = CreatePotentialBreach("TestRule");
        SetupDetectorReturnsBreaches(potentialBreach);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("SampleBreachMonitoredRequest");
    }

    [Fact]
    public async Task Handle_BreachDetected_BlockMode_CallsHandlerForEachBreach()
    {
        // Arrange
        var breach1 = CreatePotentialBreach("Rule1");
        var breach2 = CreatePotentialBreach("Rule2");
        SetupDetectorReturnsBreaches(breach1, breach2);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await _handler.Received(2)
            .HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleBreaches_BlockMode_ErrorContainsAllRuleNames()
    {
        // Arrange
        var breach1 = CreatePotentialBreach("RuleAlpha");
        var breach2 = CreatePotentialBreach("RuleBeta");
        SetupDetectorReturnsBreaches(breach1, breach2);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("RuleAlpha");
        error.Message.Should().Contain("RuleBeta");
    }

    #endregion

    #region Breach Detected — Warn Mode

    [Fact]
    public async Task Handle_BreachDetected_WarnMode_ReturnsOriginalResult()
    {
        // Arrange
        var potentialBreach = CreatePotentialBreach("TestRule");
        SetupDetectorReturnsBreaches(potentialBreach);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BreachDetected_WarnMode_StillCallsHandler()
    {
        // Arrange
        var potentialBreach = CreatePotentialBreach("TestRule");
        SetupDetectorReturnsBreaches(potentialBreach);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await _handler.Received(1)
            .HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Detection Failure

    [Fact]
    public async Task Handle_DetectionFailed_BlockMode_ReturnsError()
    {
        // Arrange
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                Left<EncinaError, IReadOnlyList<PotentialBreach>>(
                    EncinaError.New("Detection system unavailable"))));

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DetectionFailed_WarnMode_ReturnsOriginalResult()
    {
        // Arrange
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                Left<EncinaError, IReadOnlyList<PotentialBreach>>(
                    EncinaError.New("Detection system unavailable"))));

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task Handle_DetectorThrows_BlockMode_ReturnsDetectionFailedError()
    {
        // Arrange
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<PotentialBreach>>>>(
                _ => throw new InvalidOperationException("Detector crashed"));

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().Match(
            Some: code => code.Should().Be(BreachNotificationErrors.DetectionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task Handle_DetectorThrows_WarnMode_ReturnsOriginalResult()
    {
        // Arrange
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<PotentialBreach>>>>(
                _ => throw new InvalidOperationException("Detector crashed"));

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region SecurityEvent Creation

    [Fact]
    public async Task Handle_MonitoredRequest_PassesCorrectEventTypeToDetector()
    {
        // Arrange
        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");
        SecurityEvent? capturedEvent = null;

        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedEvent = callInfo.Arg<SecurityEvent>();
                return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                    Right<EncinaError, IReadOnlyList<PotentialBreach>>(
                        (IReadOnlyList<PotentialBreach>)[]));
            });

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(userId: "user-1"), Next(Unit.Default), CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EventType.Should().Be(SecurityEventType.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_MonitoredRequest_UsesTimeProviderForEventTimestamp()
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetUtcNow(expectedTime);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");
        SecurityEvent? capturedEvent = null;

        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedEvent = callInfo.Arg<SecurityEvent>();
                return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                    Right<EncinaError, IReadOnlyList<PotentialBreach>>(
                        (IReadOnlyList<PotentialBreach>)[]));
            });

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(userId: "user-1"), Next(Unit.Default), CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.OccurredAtUtc.Should().Be(expectedTime);
    }

    [Fact]
    public async Task Handle_MonitoredRequest_SecurityEventIncludesRequestTypeAsSource()
    {
        // Arrange
        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");
        SecurityEvent? capturedEvent = null;

        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedEvent = callInfo.Arg<SecurityEvent>();
                return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                    Right<EncinaError, IReadOnlyList<PotentialBreach>>(
                        (IReadOnlyList<PotentialBreach>)[]));
            });

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(userId: "user-1"), Next(Unit.Default), CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Source.Should().Contain("SampleBreachMonitoredRequest");
    }

    #endregion

    #region NextStep Execution

    [Fact]
    public async Task Handle_MonitoredRequest_ExecutesNextStep()
    {
        // Arrange
        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> next = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        };

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MonitoredRequest_ExecutesNextStepBeforeDetection()
    {
        // Arrange -- verify the handler is called (which proves nextStep ran first)
        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert -- detector is called, which means nextStep completed first
        await _detector.Received(1)
            .DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EnforcementDisabled_StillInvokesNextStep()
    {
        // Arrange
        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Disabled);
        var request = new SampleBreachMonitoredRequest("user-1");
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> next = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        };

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoAttribute_StillInvokesNextStep()
    {
        // Arrange
        var behavior = CreateUnmonitoredBehavior();
        var request = new SampleUnmonitoredRequest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> next = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        };

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), next, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
    }

    #endregion

    #region Cancellation Token Forwarding

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToDetector()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        await behavior.Handle(request, RequestContext.CreateForTest(), Next(Unit.Default), token);

        // Assert
        await _detector.Received(1)
            .DetectAsync(Arg.Any<SecurityEvent>(), token);
    }

    [Fact]
    public async Task Handle_BreachDetected_ForwardsCancellationTokenToHandler()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var potentialBreach = CreatePotentialBreach("TestRule");
        SetupDetectorReturnsBreaches(potentialBreach);

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        await behavior.Handle(request, RequestContext.CreateForTest(), Next(Unit.Default), token);

        // Assert
        await _handler.Received(1)
            .HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), token);
    }

    #endregion

    #region Detection Failure -- Handler Not Called

    [Fact]
    public async Task Handle_DetectionFailed_DoesNotCallHandler()
    {
        // Arrange
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                Left<EncinaError, IReadOnlyList<PotentialBreach>>(
                    EncinaError.New("Detection system unavailable"))));

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await _handler.DidNotReceive()
            .HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DetectionFailed_BlockMode_ReturnsOriginalDetectionError()
    {
        // Arrange
        var detectionError = EncinaError.New("Detection system unavailable");
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                Left<EncinaError, IReadOnlyList<PotentialBreach>>(detectionError)));

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        var error = (EncinaError)result;
        error.Message.Should().Be("Detection system unavailable");
    }

    #endregion

    #region Exception Handling -- Additional Verifications

    [Fact]
    public async Task Handle_DetectorThrows_BlockMode_ErrorMessageContainsRequestTypeName()
    {
        // Arrange
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<PotentialBreach>>>>(
                _ => throw new InvalidOperationException("Detector crashed"));

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Block);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        var error = (EncinaError)result;
        error.Message.Should().Contain("SampleBreachMonitoredRequest");
    }

    [Fact]
    public async Task Handle_DetectorThrows_WarnMode_DoesNotCallHandler()
    {
        // Arrange
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<PotentialBreach>>>>(
                _ => throw new InvalidOperationException("Detector crashed"));

        var behavior = CreateMonitoredBehavior(
            o => o.EnforcementMode = BreachDetectionEnforcementMode.Warn);
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await _handler.DidNotReceive()
            .HandleDetectedBreachAsync(Arg.Any<PotentialBreach>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Default Enforcement Mode

    [Fact]
    public async Task Handle_DefaultOptions_UsesWarnMode()
    {
        // Arrange -- BreachNotificationOptions defaults to Warn mode
        var potentialBreach = CreatePotentialBreach("TestRule");
        SetupDetectorReturnsBreaches(potentialBreach);

        var behavior = CreateMonitoredBehavior(); // no custom configuration
        var request = new SampleBreachMonitoredRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert -- Warn mode returns the original result even when breach detected
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        // Arrange
        var behavior = CreateMonitoredBehavior();

        // Act
        var act = async () => await behavior.Handle(
            null!, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    #endregion

    #region Helpers

    private static RequestHandlerCallback<Unit> Next(Unit value) =>
        () => ValueTask.FromResult<Either<EncinaError, Unit>>(value);

    private static PotentialBreach CreatePotentialBreach(
        string ruleName = "TestRule",
        BreachSeverity severity = BreachSeverity.High)
    {
        return new PotentialBreach
        {
            DetectionRuleName = ruleName,
            Severity = severity,
            Description = $"Breach detected by {ruleName}",
            SecurityEvent = SecurityEvent.Create(
                SecurityEventType.UnauthorizedAccess,
                "test-source",
                "test description",
                DateTimeOffset.UtcNow),
            DetectedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private void SetupDetectorReturnsBreaches(params PotentialBreach[] breaches)
    {
        _detector.DetectAsync(Arg.Any<SecurityEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PotentialBreach>>>(
                Right<EncinaError, IReadOnlyList<PotentialBreach>>(
                    (IReadOnlyList<PotentialBreach>)breaches)));
    }

    private BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit> CreateMonitoredBehavior(
        Action<BreachNotificationOptions>? configure = null)
    {
        var options = new BreachNotificationOptions();
        configure?.Invoke(options);
        return new BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>(
            _detector,
            _handler,
            Options.Create(options),
            _timeProvider,
            NullLogger<BreachDetectionPipelineBehavior<SampleBreachMonitoredRequest, Unit>>.Instance);
    }

    private BreachDetectionPipelineBehavior<SampleUnmonitoredRequest, Unit> CreateUnmonitoredBehavior(
        Action<BreachNotificationOptions>? configure = null)
    {
        var options = new BreachNotificationOptions();
        configure?.Invoke(options);
        return new BreachDetectionPipelineBehavior<SampleUnmonitoredRequest, Unit>(
            _detector,
            _handler,
            Options.Create(options),
            _timeProvider,
            NullLogger<BreachDetectionPipelineBehavior<SampleUnmonitoredRequest, Unit>>.Instance);
    }

    #endregion
}

// Test request types for breach detection pipeline behavior tests

/// <summary>
/// Test command decorated with <see cref="BreachMonitoredAttribute"/> for pipeline behavior tests.
/// Uses <see cref="SecurityEventType.UnauthorizedAccess"/> as the monitored event type.
/// </summary>
[BreachMonitored(EventType = SecurityEventType.UnauthorizedAccess)]
public sealed record SampleBreachMonitoredRequest(string UserId) : ICommand<Unit>;

/// <summary>
/// Test command WITHOUT <see cref="BreachMonitoredAttribute"/> for passthrough tests.
/// The pipeline behavior should skip detection entirely for this type.
/// </summary>
public sealed record SampleUnmonitoredRequest : ICommand<Unit>;
