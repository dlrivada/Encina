using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests;

[Collection("NonParallel")]
public sealed class EncinaTests
{
    private static readonly string[] expected = new[] { "tracking:before", "second:before", "handler", "second:after", "tracking:after" };

    [Fact]
    public async Task Send_ExecutesHandlersThroughConfiguredPipeline()
    {
        var tracker = new PipelineTracker();

        var services = new ServiceCollection();
        services.AddEncina(cfg =>
        {
            cfg.AddPipelineBehavior(typeof(TrackingBehavior<,>))
               .AddPipelineBehavior(typeof(SecondTrackingBehavior<,>));
        });
        services.AddScoped<IRequestHandler<EchoRequest, string>, EchoRequestHandler>();
        services.AddScoped(_ => tracker);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoRequest("hello"), CancellationToken.None);

        var response = ExpectSuccess(result);
        response.ShouldBe("hello");
        tracker.Events.ShouldBe(expected);
    }

    [Fact]
    public async Task Send_EmitsActivityWithMetadata()
    {
        using var activityCollector = new ActivityCollector();
        var services = BuildServiceCollection();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoRequest("hi"), CancellationToken.None);

        var response = ExpectSuccess(result);
        response.ShouldBe("hi");
        var activity = activityCollector.Activities.Last(
            a => a.DisplayName == "Encina.Send"
                 && Equals(a.GetTagItem("mediator.request_type"), typeof(EchoRequest).FullName));
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("mediator.request_type").ShouldBe(typeof(EchoRequest).FullName);
        activity.GetTagItem("mediator.request_name").ShouldBe(nameof(EchoRequest));
        activity.GetTagItem("mediator.response_type").ShouldBe(typeof(string).FullName);
        activity.GetTagItem("mediator.request_kind").ShouldBe("request");
        activity.GetTagItem("mediator.handler").ShouldNotBeNull();
        activity.GetTagItem("mediator.handler_count").ShouldBe(1);
    }

    [Fact]
    public async Task Send_EmitsActivityWithFailureMetadata_WhenHandlerMissing()
    {
        using var activityCollector = new ActivityCollector();
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EchoRequest).Assembly);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new MissingHandlerRequest(), CancellationToken.None);

        var error = ExpectFailure(result, MediatorErrorCodes.RequestHandlerMissing);
        var activity = activityCollector.Activities.Last(
            a => a.DisplayName == "Encina.Send"
                 && Equals(a.GetTagItem("mediator.request_type"), typeof(MissingHandlerRequest).FullName));

        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe(error.Message);
        activity.GetTagItem("mediator.request_type").ShouldBe(typeof(MissingHandlerRequest).FullName);
        activity.GetTagItem("mediator.request_name").ShouldBe(nameof(MissingHandlerRequest));
        activity.GetTagItem("mediator.request_kind").ShouldBe("request");
        activity.GetTagItem("mediator.failure_reason").ShouldBe(error.GetMediatorCode());
    }

    [Fact]
    public async Task Send_ReturnsFailureWhenRequestIsNull()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send<string>(null!, CancellationToken.None);

        var error = ExpectFailure(result, "mediator.request.null");
        error.Message.ShouldContain("cannot be null");
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("The request cannot be null."));
    }

    [Fact]
    public async Task Send_ReturnsFailure_WhenHandlerNotRegistered()
    {
        var loggerCollector = new LoggerCollector();
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EchoRequest).Assembly);
        services.AddSingleton(loggerCollector);
        services.AddSingleton<ILogger<Encina>>(sp => new ListLogger<Encina>(sp.GetRequiredService<LoggerCollector>()));
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new MissingHandlerRequest(), CancellationToken.None);

        var error = ExpectFailure(result, MediatorErrorCodes.RequestHandlerMissing);
        error.Message.ShouldContain("No registered IRequestHandler was found");
        error.Message.ShouldContain(nameof(MissingHandlerRequest));
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("No registered IRequestHandler was found for")
            && entry.Message.Contains(nameof(MissingHandlerRequest)));
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_ReturnsFailure_WhenHandlerThrows()
    {
        var services = BuildServiceCollection();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new FaultyRequest(), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.handler.exception");
        error.Message.ShouldContain("Unexpected exception");
        var exception = ExtractException(error);
        exception.ShouldNotBeNull();
        exception!.Message.ShouldBe("boom");
    }

    [Fact]
    public async Task Send_DetectsCancellation()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(pipelineTracker: new PipelineTracker(), loggerCollector: loggerCollector);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Send(new CancellableRequest(), cts.Token);

        var error = ExpectFailure(result, "mediator.handler.cancelled");
        error.Message.ShouldContain("was cancelled");
        error.Message.ShouldContain(nameof(CancellableRequest));
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(CancellableRequest)));
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_DoesNotLogCancellation_WhenHandlerCancelsWithoutToken()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        services.RemoveAll(typeof(IRequestHandler<AccidentalCancellationRequest, string>));
        services.AddScoped<IRequestHandler<AccidentalCancellationRequest, string>, AccidentalCancellationRequestHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new AccidentalCancellationRequest(), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.handler.exception");
        ExtractException(error).ShouldBeOfType<OperationCanceledException>();
        loggerCollector.Entries.Any(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(AccidentalCancellationRequest))).ShouldBeFalse();
    }

    [Fact]
    public async Task Send_RecordsSuccessMetrics()
    {
        var metrics = new MediatorMetricsSpy();
        var services = BuildServiceCollection(mediatorMetrics: metrics);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoRequest("hello"), CancellationToken.None);

        var response = ExpectSuccess(result);
        response.ShouldBe("hello");

        var success = metrics.Successes.ShouldHaveSingleItem();
        success.Kind.ShouldBe("request");
        success.Name.ShouldBe(nameof(EchoRequest));
        success.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        metrics.Failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_RecordsFailureMetrics_WhenHandlerMissing()
    {
        var metrics = new MediatorMetricsSpy();
        var services = BuildServiceCollection(mediatorMetrics: metrics);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new MissingHandlerRequest(), CancellationToken.None);

        var error = ExpectFailure(result, MediatorErrorCodes.RequestHandlerMissing);
        var failure = metrics.Failures.ShouldHaveSingleItem();
        failure.Kind.ShouldBe("request");
        failure.Name.ShouldBe(nameof(MissingHandlerRequest));
        failure.Reason.ShouldBe(error.GetMediatorCode());
        failure.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        metrics.Successes.ShouldBeEmpty();
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_RecordsFailureMetrics_WhenHandlerThrows()
    {
        var metrics = new MediatorMetricsSpy();
        var services = BuildServiceCollection(mediatorMetrics: metrics);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new FaultyRequest(), CancellationToken.None);

        var error = ExpectFailure(result, MediatorErrorCodes.HandlerException);
        var failure = metrics.Failures.ShouldHaveSingleItem();
        failure.Kind.ShouldBe("request");
        failure.Name.ShouldBe(nameof(FaultyRequest));
        failure.Reason.ShouldBe(error.GetMediatorCode());
        failure.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        metrics.Successes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_RecordsFailureMetrics_WhenCancelled()
    {
        var metrics = new MediatorMetricsSpy();
        var services = BuildServiceCollection(mediatorMetrics: metrics);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Send(new CancellableRequest(), cts.Token);

        var error = ExpectFailure(result, MediatorErrorCodes.HandlerCancelled);
        var failure = metrics.Failures.ShouldHaveSingleItem();
        failure.Kind.ShouldBe("request");
        failure.Name.ShouldBe(nameof(CancellableRequest));
        failure.Reason.ShouldBe(error.GetMediatorCode());
        failure.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        metrics.Successes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_LogsWarning_WhenErrorCodeEqualsCancelled()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        services.AddScoped<IRequestHandler<CancelledOutcomeRequest, string>, CancelledOutcomeRequestHandler>();
        services.AddScoped<IPipelineBehavior<CancelledOutcomeRequest, string>, CancelledOutcomeBehavior>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new CancelledOutcomeRequest(), CancellationToken.None);

        var error = ExpectFailure(result, "cancelled");
        error.Message.ShouldBe("Explicit cancellation.");
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(CancelledOutcomeRequest))
            && entry.Message.Contains("cancelled"));
        loggerCollector.Entries.ShouldNotContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("The request")
            && entry.Message.Contains(nameof(CancelledOutcomeRequest)));
    }

    [Fact]
    public async Task Send_TracksCancellationInMetrics_WhenDispatcherCatchesCancellation()
    {
        var metrics = new MediatorMetricsSpy();
        using var activityCollector = new ActivityCollector();

        var services = BuildServiceCollection(mediatorMetrics: metrics);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Send(new CancellableRequest(), cts.Token);

        var error = ExpectFailure(result, MediatorErrorCodes.HandlerCancelled);

        // Verify metrics tracked cancellation specifically
        var failure = metrics.Failures.ShouldHaveSingleItem();
        failure.Reason.ShouldBe(MediatorErrorCodes.HandlerCancelled);
        failure.Kind.ShouldBe("request");
        failure.Name.ShouldBe(nameof(CancellableRequest));
        metrics.Successes.ShouldBeEmpty();

        // Verify activity recorded cancellation
        var activity = activityCollector.Activities.Last(a =>
            a.DisplayName == "Encina.Send" &&
            Equals(a.GetTagItem("mediator.request_type"), typeof(CancellableRequest).FullName));
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("mediator.failure_reason").ShouldBe(MediatorErrorCodes.HandlerCancelled);
    }

    [Fact]
    public async Task LogSendOutcome_EmitsWarningWithoutErrorForCancelledPrefix()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);

        await using var provider = services.BuildServiceProvider();
        var mediator = (Encina)provider.GetRequiredService<IMediator>();

        var logSendOutcome = typeof(Encina)
            .GetMethod("LogSendOutcome", BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(string));

        var cancelledError = MediatorErrors.Create("cancelled.flow", "Flow stopped");
        var outcome = Left<MediatorError, string>(cancelledError);

        logSendOutcome.Invoke(mediator, new object[]
        {
            typeof(CancelledOutcomeRequest),
            typeof(CancelledOutcomeRequestHandler),
            outcome
        });

        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains("cancelled.flow"));
        loggerCollector.Entries.ShouldNotContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("cancelled.flow"));
    }

    [Fact]
    public async Task LogSendOutcome_LogsDebugOnSuccessfulOutcome()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);

        await using var provider = services.BuildServiceProvider();
        var mediator = (Encina)provider.GetRequiredService<IMediator>();

        var logSendOutcome = typeof(Encina)
            .GetMethod("LogSendOutcome", BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(string));

        var outcome = Right<MediatorError, string>("ok");

        logSendOutcome.Invoke(mediator, new object[]
        {
            typeof(EchoRequest),
            typeof(EchoRequestHandler),
            outcome
        });

        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Debug
            && entry.Message.Contains("Request EchoRequest completed"));
        loggerCollector.Entries.ShouldNotContain(entry =>
            entry.LogLevel == LogLevel.Warning
            || entry.LogLevel == LogLevel.Error
            || entry.LogLevel == LogLevel.Critical);
    }

    [Fact]
    public async Task LogSendOutcome_LogsErrorWithException_OnFailure()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);

        await using var provider = services.BuildServiceProvider();
        var mediator = (Encina)provider.GetRequiredService<IMediator>();

        var logSendOutcome = typeof(Encina)
            .GetMethod("LogSendOutcome", BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(string));

        var exception = new InvalidOperationException("failure");
        var error = MediatorErrors.FromException("mediator.failure", exception, "the operation failed");
        var outcome = Left<MediatorError, string>(error);

        logSendOutcome.Invoke(mediator, new object[]
        {
            typeof(EchoRequest),
            typeof(EchoRequestHandler),
            outcome
        });

        var failureEntry = loggerCollector.Entries.Single(entry => entry.LogLevel == LogLevel.Error);
        failureEntry.Message.Contains("The EchoRequest request failed (mediator.failure)").ShouldBeTrue();
        failureEntry.Message.Contains("the operation failed").ShouldBeTrue();
        failureEntry.Exception.ShouldNotBeNull();
        ReferenceEquals(failureEntry.Exception, exception).ShouldBeTrue();
        loggerCollector.Entries.Any(entry => entry.LogLevel == LogLevel.Warning).ShouldBeFalse();
    }

    [Fact]
    public async Task LogSendOutcome_LogsErrorWithoutException_OnFailure()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);

        await using var provider = services.BuildServiceProvider();
        var mediator = (Encina)provider.GetRequiredService<IMediator>();

        var logSendOutcome = typeof(Encina)
            .GetMethod("LogSendOutcome", BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(string));

        var error = MediatorErrors.Create("mediator.failure", "the operation failed");
        var outcome = Left<MediatorError, string>(error);

        logSendOutcome.Invoke(mediator, new object[]
        {
            typeof(EchoRequest),
            typeof(EchoRequestHandler),
            outcome
        });

        var failureEntry = loggerCollector.Entries.Single(entry => entry.LogLevel == LogLevel.Error);
        failureEntry.Message.ShouldContain("The EchoRequest request failed (mediator.failure)");
        failureEntry.Exception.ShouldNotBeNull();
        failureEntry.Exception!.GetType().Name.ShouldBe("MediatorException");
        failureEntry.Exception.InnerException.ShouldBeNull();
        error.Exception.IsSome.ShouldBeTrue();
        loggerCollector.Entries.Any(entry => entry.LogLevel == LogLevel.Warning).ShouldBeFalse();
    }

    [Theory]
    [InlineData("cancelled", true)]
    [InlineData("cancelled.flow", true)]
    [InlineData("mediator.handler.cancelled", true)]
    [InlineData("Mediator.Behavior.Cancelled", true)]
    [InlineData("mediator.handler.exception", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsCancellationCode_DetectsSubstring(string? code, bool expected)
    {
        Encina.IsCancellationCode(code ?? string.Empty).ShouldBe(expected);
    }

    private static readonly string[] expectedNotificationOrder = new[] { "A:42", "B:42" };

    [Fact]
    public async Task Publish_InvokesAllNotificationHandlers_AndAllowsNullResult()
    {
        var tracker = new NotificationTracker();
        var loggerCollector = new LoggerCollector();
        using var activityCollector = new ActivityCollector();
        var services = BuildServiceCollection(notificationTracker: tracker, loggerCollector: loggerCollector);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new SampleNotification(42), CancellationToken.None);

        ExpectSuccess(result);
        tracker.Handled.ShouldBe(expectedNotificationOrder);
        loggerCollector.Entries.Count(entry => entry.Message.Contains("Sending notification")).ShouldBe(2);
        activityCollector.Activities.ShouldNotBeEmpty();
        var activity = activityCollector.Activities.Last(a => a.DisplayName == "Encina.Publish");
        activity.DisplayName.ShouldBe("Encina.Publish");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("mediator.notification_type").ShouldBe(typeof(SampleNotification).FullName);
        activity.GetTagItem("mediator.notification_name").ShouldBe(nameof(SampleNotification));
        activity.GetTagItem("mediator.notification_kind").ShouldBe("notification");
        activity.GetTagItem("mediator.handler_count").ShouldBe(2);
    }

    [Fact]
    public async Task Publish_EmitsMetadataForNotificationActivity()
    {
        using var activityCollector = new ActivityCollector();
        var services = BuildServiceCollection();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new SampleNotification(7), CancellationToken.None);

        ExpectSuccess(result);
        var activity = activityCollector.Activities.Last(a => a.DisplayName == "Encina.Publish");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("mediator.notification_type").ShouldBe(typeof(SampleNotification).FullName);
        activity.GetTagItem("mediator.notification_name").ShouldBe(nameof(SampleNotification));
        activity.GetTagItem("mediator.notification_kind").ShouldBe("notification");
        activity.GetTagItem("mediator.handler_count").ShouldBe(2);
    }

    [Fact]
    public async Task Publish_SwallowsWhenNoHandlersRegistered()
    {
        var loggerCollector = new LoggerCollector();
        using var activityCollector = new ActivityCollector();
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EncinaTests).Assembly);
        services.RemoveAll(typeof(INotificationHandler<UnhandledNotification>));
        services.AddSingleton(loggerCollector);
        services.AddSingleton<ILogger<Encina>>(sp => new ListLogger<Encina>(sp.GetRequiredService<LoggerCollector>()));
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new UnhandledNotification(), CancellationToken.None);

        ExpectSuccess(result);
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Debug
            && entry.Message.Contains("No handlers were found for the")
            && entry.Message.Contains(nameof(UnhandledNotification)));
        var activity = activityCollector.Activities.Last(a => a.DisplayName == "Encina.Publish");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("mediator.notification_kind").ShouldBe("notification");
    }

    [Fact]
    public async Task Publish_ReturnsFailureWhenNotificationIsNull()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish<SampleNotification>(null!, CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.null");
        error.Message.ShouldContain("cannot be null");
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("The notification cannot be null."));
    }

    [Fact]
    public async Task Publish_ReportsRuntimeTypeWhenHandlerCancelsWithToken()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<INotificationHandler<ExplicitNotification>, CancellingExplicitNotificationHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Publish<INotification>(new ExplicitNotification(), cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain("was cancelled");
        error.Message.ShouldContain(nameof(ExplicitNotification));
        error.Message.ShouldNotContain("Publishing INotification");
    }

    [Fact]
    public void MediatorErrors_FromException_PopulatesMetadata()
    {
        var inner = new InvalidOperationException("failure");

        var error = MediatorErrors.FromException("mediator.test", inner, "boom", new { Value = 42 });

        error.Message.ShouldBe("boom");
        ExtractException(error).ShouldBe(inner);
        error.GetMediatorCode().ShouldBe("mediator.test");
        error.GetMediatorDetails().ShouldNotBeNull();
        error.GetMediatorDetails()!.ShouldBeAssignableTo<object>();
    }

    [Fact]
    public void MediatorErrors_Unknown_ExposesDefaultMessage()
    {
        var error = MediatorErrors.Unknown;

        error.GetMediatorCode().ShouldBe("mediator.unknown");
        error.Message.ShouldBe("An unexpected error occurred in Encina.");
    }


    [Fact]
    public void Error_New_WithNullException_DoesNotExposeException()
    {
        var error = MediatorError.New("message", (Exception?)null);

        error.Exception.IsSome.ShouldBeFalse();
        error.MetadataException.IsSome.ShouldBeFalse();
        error.Message.ShouldBe("message");
    }
    [Fact]
    public void Error_NewString_UsesDefaultMessageWhenBlank()
    {
        var error = MediatorError.New(string.Empty);

        error.Message.ShouldBe("An error occurred");
        error.Exception.IsSome.ShouldBeFalse();
        error.GetMediatorDetails().ShouldBeNull();
    }

    [Fact]
    public void Error_NewException_ReturnsDefaultWhenNull()
    {
        var error = MediatorError.New((Exception)null!);

        error.Message.ShouldBe("An error occurred");
        error.Exception.IsSome.ShouldBeFalse();
        error.GetMediatorDetails().ShouldBeNull();
    }

    [Fact]
    public void Error_NewException_NormalizesMediatorExceptionInner()
    {
        var inner = new InvalidOperationException("inner");
        var mediatorException = new MediatorException("mediator.code", "outer", inner, details: null);

        var error = MediatorError.New(mediatorException);

        error.Message.ShouldBe("outer");
        ExtractException(error).ShouldBe(inner);
        error.GetMediatorCode().ShouldBe("mediator.code");
        error.GetMediatorDetails().ShouldBeNull();
    }

    [Fact]
    public void Error_NewExceptionWithOverride_PreservesMetadata()
    {
        var mediatorException = new MediatorException("mediator.override", "outer", innerException: null, details: 99);

        var error = MediatorError.New(mediatorException, "override message");

        error.Message.ShouldBe("override message");
        ExtractException(error).ShouldBe(mediatorException);
        error.GetMediatorCode().ShouldBe("mediator.override");
        error.GetMediatorDetails().ShouldBe(99);
    }

    [Fact]
    public void Error_NewMessageAndException_PreservesException()
    {
        var exception = new InvalidOperationException("oops");

        var error = MediatorError.New("boom", exception);

        error.Message.ShouldBe("boom");
        ExtractException(error).ShouldBe(exception);
        error.GetMediatorCode().ShouldBe(nameof(InvalidOperationException));
        error.GetMediatorDetails().ShouldBeNull();
    }

    [Fact]
    public async Task Publish_PropagatesHandlerFailures()
    {
        var tracker = new NotificationTracker();
        var loggerCollector = new LoggerCollector();
        using var activityCollector = new ActivityCollector();
        var services = BuildServiceCollection(notificationTracker: tracker, loggerCollector: loggerCollector);
        services.AddScoped<INotificationHandler<SampleNotification>, FaultyNotificationHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new SampleNotification(7), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.exception");
        var publishException = ExtractException(error);
        publishException.ShouldNotBeNull();
        publishException!.Message.ShouldBe("notify-failure");
        loggerCollector.Entries.Any(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Exception != null
            && entry.Exception.Message == "notify-failure"
            && entry.Message.Contains("Error while publishing notification")
            && entry.Message.Contains(nameof(SampleNotification))).ShouldBeTrue();
        var activity = activityCollector.Activities.Last(a => a.DisplayName == "Encina.Publish");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe(error.Message);
        activity.GetTagItem("mediator.failure_reason").ShouldBe(error.GetMediatorCode());
    }

    [Fact]
    public async Task Publish_StopsProcessingHandlersAfterFailure()
    {
        var tracker = new NotificationTracker();
        var loggerCollector = new LoggerCollector();
        using var activityCollector = new ActivityCollector();
        var services = BuildServiceCollection(notificationTracker: tracker, loggerCollector: loggerCollector);
        services.RemoveAll(typeof(INotificationHandler<SampleNotification>));
        services.AddScoped<INotificationHandler<SampleNotification>, FaultyNotificationHandler>();
        services.AddScoped<INotificationHandler<SampleNotification>, AsyncSampleNotificationHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new SampleNotification(21), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.exception");
        tracker.Handled.ShouldBeEmpty();
        loggerCollector.Entries.Count(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("Error while publishing notification")
            && entry.Message.Contains(nameof(SampleNotification))).ShouldBe(1);
        var activity = activityCollector.Activities.Last(a => a.DisplayName == "Encina.Publish");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe(error.Message);
        activity.GetTagItem("mediator.failure_reason").ShouldBe(error.GetMediatorCode());
    }

    [Fact]
    public async Task Publish_AllowsHandlersReturningNullTasks()
    {
        var services = BuildServiceCollection();
        services.AddScoped<INotificationHandler<SampleNotification>, NullReturningNotificationHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new SampleNotification(99), CancellationToken.None);

        // In pure ROP, returning null! from a handler causes NullReferenceException
        var error = ExpectFailure(result, "mediator.notification.exception");
        error.Message.ShouldContain("Unexpected exception");
        ExtractException(error).ShouldBeOfType<NullReferenceException>();
    }

    [Fact]
    public async Task Publish_SkipsNullHandlers_AndContinuesWithValidHandlers()
    {
        var tracker = new NotificationTracker();
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped(_ => tracker);

        // Register valid handler
        services.AddScoped<INotificationHandler<SampleNotification>, AsyncSampleNotificationHandler>();

        // Register a factory that returns null (simulating DI returning null handler)
        services.AddScoped<INotificationHandler<SampleNotification>>(_ => null!);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new SampleNotification(42), CancellationToken.None);

        ExpectSuccess(result);
        // The valid handler should have processed despite the null handler
        tracker.Handled.ShouldContain("Async:42");
    }

    [Fact]
    public async Task Publish_HonorsCancellationRequests()
    {
        var loggerCollector = new LoggerCollector();
        using var activityCollector = new ActivityCollector();
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EchoRequest).Assembly);
        services.RemoveAll(typeof(INotificationHandler<SampleNotification>));
        services.AddScoped<INotificationHandler<SampleNotification>, CancellableNotificationHandler>();
        services.AddSingleton(loggerCollector);
        services.AddSingleton<ILogger<Encina>>(sp => new ListLogger<Encina>(sp.GetRequiredService<LoggerCollector>()));

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Publish(new SampleNotification(5), cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain("was cancelled");
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(SampleNotification)));
        var activity = activityCollector.Activities.Last(a => a.DisplayName == "Encina.Publish");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe(error.Message);
        activity.GetTagItem("mediator.failure_reason").ShouldBe(error.GetMediatorCode());
    }

    [Fact]
    public async Task Publish_LogsWarningWithoutError_WhenCancelledBeforeHandlersRun()
    {
        var loggerCollector = new LoggerCollector();
        using var activityCollector = new ActivityCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        services.RemoveAll(typeof(INotificationHandler<SampleNotification>));
        services.AddScoped<INotificationHandler<SampleNotification>, CancellableNotificationHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Publish(new SampleNotification(27), cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain("was cancelled");
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(SampleNotification)));
        loggerCollector.Entries.Any(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("Error while publishing notification")
            && entry.Message.Contains(nameof(SampleNotification))).ShouldBeFalse();
        var activity = activityCollector.Activities.Last(a => a.DisplayName == "Encina.Publish");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe(error.Message);
        activity.GetTagItem("mediator.failure_reason").ShouldBe(error.GetMediatorCode());
    }

    [Fact]
    public async Task Publish_DoesNotLogCancellation_WhenHandlerCancelsWithoutToken()
    {
        var loggerCollector = new LoggerCollector();
        using var activityCollector = new ActivityCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        services.RemoveAll(typeof(INotificationHandler<AccidentalCancellationNotification>));
        services.AddScoped<INotificationHandler<AccidentalCancellationNotification>, AccidentalCancellationNotificationHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new AccidentalCancellationNotification(), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.exception");
        ExtractException(error).ShouldBeOfType<OperationCanceledException>();
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("Error while publishing notification")
            && entry.Message.Contains(nameof(AccidentalCancellationNotification)));
        loggerCollector.Entries.Any(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(AccidentalCancellationNotification))).ShouldBeFalse();
        var activity = activityCollector.Activities.Last(a => a.DisplayName == "Encina.Publish");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe(error.Message);
        activity.GetTagItem("mediator.failure_reason").ShouldBe(error.GetMediatorCode());
    }

    [Fact]
    public async Task Publish_ReturnsFailureWhenHandlerDoesNotExposePublicHandle()
    {
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EncinaTests).Assembly);
        services.RemoveAll(typeof(INotificationHandler<ExplicitNotification>));
        services.AddScoped<INotificationHandler<ExplicitNotification>, ExplicitInterfaceNotificationHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new ExplicitNotification(), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.missing_handle");
        error.Message.ShouldContain(nameof(ExplicitInterfaceNotificationHandler));
        error.Message.ShouldContain("does not expose a compatible Handle method");

        var metadata = error.GetMediatorMetadata();
        metadata.ShouldContainKey("handler");
        metadata["handler"].ShouldBe(typeof(ExplicitInterfaceNotificationHandler).FullName);
        metadata.ShouldContainKey("expectedNotification");
        metadata["expectedNotification"].ShouldBe(typeof(ExplicitNotification).FullName);
        metadata.ShouldContainKey("notification");
        metadata["notification"].ShouldBe(nameof(ExplicitNotification));
    }

    [Fact]
    public async Task InvokeNotificationHandler_ReportsFailureWhenHandlerThrowsSynchronously()
    {
        var method = typeof(Encina)
            .GetMethod("InvokeNotificationHandler", BindingFlags.NonPublic | BindingFlags.Static)!.
            MakeGenericMethod(typeof(ExplicitNotification));
        var invoke = (Func<object, ExplicitNotification, CancellationToken, Task<Either<MediatorError, Unit>>>)method.CreateDelegate(typeof(Func<object, ExplicitNotification, CancellationToken, Task<Either<MediatorError, Unit>>>));

        var handler = new ThrowingExplicitNotificationHandler();
        var result = await invoke(handler, new ExplicitNotification(), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.exception");
        var explicitException = ExtractException(error);
        explicitException.ShouldNotBeNull();
        explicitException!.Message.ShouldBe("explicit boom");
    }

    [Fact]
    public async Task InvokeNotificationHandler_ReportsFailureWhenResultIsUnexpected()
    {
        var method = typeof(Encina)
            .GetMethod("InvokeNotificationHandler", BindingFlags.NonPublic | BindingFlags.Static)!.
            MakeGenericMethod(typeof(ExplicitNotification));
        var invoke = (Func<object, ExplicitNotification, CancellationToken, Task<Either<MediatorError, Unit>>>)method.CreateDelegate(typeof(Func<object, ExplicitNotification, CancellationToken, Task<Either<MediatorError, Unit>>>));

        var handler = new InvalidNotificationResultHandler();
        var result = await invoke(handler, new ExplicitNotification(), CancellationToken.None);

        // In pure ROP, invalid return type is detected during validation
        var error = ExpectFailure(result, "mediator.notification.invalid_return");
        error.Message.ShouldContain(nameof(InvalidNotificationResultHandler));

        var metadata = error.GetMediatorMetadata();
        metadata.ShouldContainKey("handler");
        metadata["handler"].ShouldBe(typeof(InvalidNotificationResultHandler).FullName);
        metadata["notification"].ShouldBe(nameof(ExplicitNotification));
        metadata.ShouldContainKey("actualReturnType");
        metadata["actualReturnType"].ShouldBe(typeof(string).FullName);
    }

    [Fact]
    public async Task InvokeNotificationHandler_UsesRuntimeType_WhenInterfacePublishReturnsUnexpectedResult()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        INotification notification = new ExplicitNotification();

        var handler = new InvalidNotificationResultHandler();
        var result = await invoke(handler, notification, CancellationToken.None);

        // In pure ROP, invalid return type is detected during validation
        // The error message contains handler name, not notification name
        var error = ExpectFailure(result, "mediator.notification.invalid_return");
        error.Message.ShouldContain(nameof(InvalidNotificationResultHandler));
    }

    [Fact]
    public async Task InvokeNotificationHandler_UsesRuntimeType_WhenInterfacePublishCancelsInsideHandler()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        INotification notification = new AccidentalCancellationNotification();
        var handler = new AccidentalCancellationNotificationHandler();
        var result = await invoke(handler, notification, cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain(nameof(AccidentalCancellationNotification));
        ExtractException(error).ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task InvokeNotificationHandler_UsesRuntimeType_WhenInnerExceptionIsRaised()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        INotification notification = new ExplicitNotification();

        var handler = new ThrowingExplicitNotificationHandler();
        var result = await invoke(handler, notification, CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.exception");
        error.Message.ShouldContain(nameof(ExplicitNotification));
        var exception = ExtractException(error);
        exception.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task InvokeNotificationHandler_UsesRuntimeType_WhenTaskCancelsAfterInvocation()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        INotification notification = new ExplicitNotification();
        var handler = new TaskCancellingExplicitNotificationHandler();
        var result = await invoke(handler, notification, cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain("was cancelled");
        error.Message.ShouldContain(nameof(ExplicitNotification));
        error.Message.ShouldNotContain("Publishing INotification");
        ExtractException(error).ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task InvokeNotificationHandler_ExposesMetadata_WhenCancelledDuringInvoke()
    {
        var invoke = CreateInvokeNotificationDelegate<ExplicitNotification>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new CancellingExplicitNotificationHandler();
        var result = await invoke(handler, new ExplicitNotification(), cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        var metadata = error.GetMediatorMetadata();
        metadata.ShouldContainKey("handler");
        metadata!["handler"].ShouldBe(typeof(CancellingExplicitNotificationHandler).FullName);
        metadata.ShouldContainKey("notification");
        metadata["notification"].ShouldBe(nameof(ExplicitNotification));
        metadata.ShouldContainKey("stage");
        // In pure ROP, all exceptions during handler are caught at handler level
        metadata["stage"].ShouldBe("handler");
    }

    [Fact]
    public async Task InvokeNotificationHandler_ExposesMetadata_WhenTaskCancelledDuringExecution()
    {
        var invoke = CreateInvokeNotificationDelegate<ExplicitNotification>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new TaskCancellingExplicitNotificationHandler();
        var result = await invoke(handler, new ExplicitNotification(), cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        var metadata = error.GetMediatorMetadata();
        metadata.ShouldContainKey("handler");
        metadata!["handler"].ShouldBe(typeof(TaskCancellingExplicitNotificationHandler).FullName);
        metadata.ShouldContainKey("notification");
        metadata["notification"].ShouldBe(nameof(ExplicitNotification));
        metadata.ShouldContainKey("stage");
        // In pure ROP, cancellation during handler execution is caught at handler level
        metadata["stage"].ShouldBe("handler");
    }

    [Fact]
    public async Task InvokeNotificationHandler_UsesGenericTypeName_WhenNotificationInstanceIsNull()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        var handler = new InvalidNotificationResultHandler();

        var result = await invoke(handler, null!, CancellationToken.None);

        // In pure ROP, invalid return type is detected during validation
        // The error message contains handler name, not notification name
        var error = ExpectFailure(result, "mediator.notification.invalid_return");
        error.Message.ShouldContain(nameof(InvalidNotificationResultHandler));
    }

    [Fact]
    public async Task InvokeNotificationHandler_UsesGenericTypeName_WhenHandlerCancelsWithTargetInvocation()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new AccidentalCancellationNotificationHandler();
        var result = await invoke(handler, null!, cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain(nameof(INotification));
    }

    [Fact]
    public async Task InvokeNotificationHandler_UsesGenericTypeName_WhenAwaitedTaskCancels()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new TaskCancellingExplicitNotificationHandler();
        var result = await invoke(handler, null!, cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain(nameof(INotification));
    }

    [Fact]
    public async Task InvokeNotificationHandler_ReportsInvocationFailure_IncludesHandlerName()
    {
        var method = typeof(Encina)
            .GetMethod("InvokeNotificationHandler", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(ExplicitNotification));
        var invoke = (Func<object, ExplicitNotification, CancellationToken, Task<Either<MediatorError, Unit>>>)method.CreateDelegate(
            typeof(Func<object, ExplicitNotification, CancellationToken, Task<Either<MediatorError, Unit>>>));

        var handler = new MissingCancellationTokenNotificationHandler();
        var result = await invoke(handler, new ExplicitNotification(), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.invoke_exception");
        error.Message.ShouldContain(nameof(MissingCancellationTokenNotificationHandler));
    }

    [Fact]
    public async Task InvokeNotificationHandler_ReturnsSuccessWhenHandlerReturnsNull()
    {
        var method = typeof(Encina)
            .GetMethod("InvokeNotificationHandler", BindingFlags.NonPublic | BindingFlags.Static)!.
            MakeGenericMethod(typeof(SampleNotification));
        var invoke = (Func<object, SampleNotification, CancellationToken, Task<Either<MediatorError, Unit>>>)method.CreateDelegate(typeof(Func<object, SampleNotification, CancellationToken, Task<Either<MediatorError, Unit>>>));

        var handler = new NullReturningNotificationHandler();
        var result = await invoke(handler, new SampleNotification(33), CancellationToken.None);

        // In pure ROP, returning null! from a handler causes NullReferenceException
        var error = ExpectFailure(result, "mediator.notification.exception");
        error.Message.ShouldContain("Unexpected exception");
        ExtractException(error).ShouldBeOfType<NullReferenceException>();
    }

    [Fact]
    public async Task InvokeNotificationHandler_CancelledInnerException_UsesRuntimeTypeName()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        var handler = new ThrowingCancelledExplicitNotificationHandler();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var notification = new ExplicitNotification();

        var result = await invoke(handler, notification, cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain(nameof(ExplicitNotification));
    }

    [Fact]
    public async Task InvokeNotificationHandler_Exception_UsesRuntimeTypeName()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        var handler = new ThrowingExplicitNotificationHandler();
        var notification = new ExplicitNotification();

        var result = await invoke(handler, notification, CancellationToken.None);

        var error = ExpectFailure(result, "mediator.notification.exception");
        error.Message.ShouldContain("Unexpected exception");
        error.Message.ShouldContain(nameof(ExplicitNotification));
        error.Message.ShouldNotContain("INotification");
    }

    [Fact]
    public async Task InvokeNotificationHandler_CancellationDuringAwait_UsesRuntimeTypeName()
    {
        var invoke = CreateInvokeNotificationDelegate<INotification>();
        var handler = new TaskCancellingExplicitNotificationHandler();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var notification = new ExplicitNotification();

        var result = await invoke(handler, notification, cts.Token);

        var error = ExpectFailure(result, "mediator.notification.cancelled");
        error.Message.ShouldContain("was cancelled");
        error.Message.ShouldContain(nameof(ExplicitNotification));
        error.Message.ShouldNotContain("Publishing INotification");
    }

    [Fact]
    public async Task Send_WritesDiagnosticLogs()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoRequest("hello"), CancellationToken.None);

        ExpectSuccess(result);

        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Debug
            && entry.Message.Contains("Processing EchoRequest with"));
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Debug
            && entry.Message.Contains("Request EchoRequest completed by"));
    }

    private static readonly string[] expectedLifecycleEvents = new[] { "pre", "handler", "post" };

    [Fact]
    public async Task Send_InvokesPreAndPostProcessors()
    {
        var lifecycleTracker = new LifecycleTracker();
        var services = BuildServiceCollection();
        services.AddScoped(_ => lifecycleTracker);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new LifecycleRequest("in"), CancellationToken.None);

        var response = ExpectSuccess(result);
        response.ShouldBe("in:ok");
        lifecycleTracker.Events.ShouldBe(expectedLifecycleEvents);
    }

    private static readonly string[] expectedPostProcessorEvents = new[] { "handler", "post:value:handled" };

    [Fact]
    public async Task Send_ReturnsSuccess_WhenPostProcessorsDoNotFail()
    {
        var tracker = new PostProcessorTracker();
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped(_ => tracker);
        services.AddScoped<IRequestHandler<PostProcessorRequest, string>, PostProcessorRequestHandler>();
        services.AddScoped<IRequestPostProcessor<PostProcessorRequest, string>, RecordingPostProcessor>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new PostProcessorRequest("value"), CancellationToken.None);

        var response = ExpectSuccess(result);
        response.ShouldBe("value:handled");
        tracker.Events.ShouldBe(expectedPostProcessorEvents);
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_LogsErrorWhenHandlerThrows()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new FaultyRequest(), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.handler.exception");
        ExtractException(error).ShouldBeOfType<InvalidOperationException>();
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("The FaultyRequest request failed (mediator.handler.exception)")
            && entry.Message.Contains("FaultyRequestHandler"));
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_ReturnsFailureWhenHandlerReturnsNullTask()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        services.RemoveAll(typeof(IRequestHandler<NullTaskRequest, string>));
        services.AddScoped<IRequestHandler<NullTaskRequest, string>, NullTaskRequestHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new NullTaskRequest("oops"), CancellationToken.None);

        // In pure ROP, returning null! from a handler causes NullReferenceException
        var error = ExpectFailure(result, "mediator.handler.exception");
        ExtractException(error).ShouldBeOfType<NullReferenceException>();
        error.Message.ShouldContain("Unexpected exception");
        error.Message.ShouldContain(nameof(NullTaskRequestHandler));
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("mediator.handler.exception")
            && entry.Message.Contains(nameof(NullTaskRequestHandler)));
    }

    [Fact]
    public async Task Send_CachesRequestHandlerWrappersForSubsequentCalls()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<CacheProbeRequest, string>, CacheProbeRequestHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var cacheField = typeof(global::Encina.Encina)
            .GetField("RequestHandlerCache", BindingFlags.Static | BindingFlags.NonPublic)!;
        var cache = cacheField.GetValue(null)!;
        var cacheType = cache.GetType();
        var key = (typeof(CacheProbeRequest), typeof(string));
        var keyType = key.GetType();
        var valueType = cacheType.GetGenericArguments()[1];

        var tryRemove = cacheType.GetMethod("TryRemove", new[] { keyType, valueType.MakeByRefType() })!;
        var removeArgs = new object?[] { key, null };
        tryRemove.Invoke(cache, removeArgs);

        var containsKey = cacheType.GetMethod("ContainsKey", new[] { keyType })!;
        ((bool)containsKey.Invoke(cache, new object[] { key })!).ShouldBeFalse();

        var first = await mediator.Send(new CacheProbeRequest("first"), CancellationToken.None);
        ExpectSuccess(first).ShouldBe("first:cached");

        ((bool)containsKey.Invoke(cache, new object[] { key })!).ShouldBeTrue();

        var countProperty = cacheType.GetProperty("Count")!;
        var countAfterFirst = (int)countProperty.GetValue(cache)!;

        var second = await mediator.Send(new CacheProbeRequest("second"), CancellationToken.None);
        ExpectSuccess(second).ShouldBe("second:cached");

        var countAfterSecond = (int)countProperty.GetValue(cache)!;
        countAfterSecond.ShouldBe(countAfterFirst);
    }

    [Fact]
    public async Task Send_LogsWarning_WhenPipelineCancelsWithExplicitCode()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(
            loggerCollector: loggerCollector,
            configuration: cfg => cfg.AddPipelineBehavior(typeof(CancelledOutcomeBehavior)));
        services.RemoveAll(typeof(IRequestHandler<CancelledOutcomeRequest, string>));
        services.AddScoped<IRequestHandler<CancelledOutcomeRequest, string>, CancelledOutcomeRequestHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new CancelledOutcomeRequest(), CancellationToken.None);

        var error = ExpectFailure(result, "cancelled");
        error.Message.ShouldContain("Explicit cancellation");
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(CancelledOutcomeRequest)));
        loggerCollector.Entries.Any(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(CancelledOutcomeRequest))).ShouldBeFalse();
    }

    [Fact]
    public async Task Send_ReturnsFailure_WhenBehaviorCancelsBeforeHandler()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(
            loggerCollector: loggerCollector,
            configuration: cfg => cfg.AddPipelineBehavior(typeof(CancellingPipelineBehavior<,>)));

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Send(new EchoRequest("value"), cts.Token);

        var error = ExpectFailure(result, "mediator.behavior.cancelled");
        error.Message.ShouldContain("cancelled");
        error.Message.ShouldContain(nameof(EchoRequest));
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("was cancelled")
            && entry.Message.Contains(nameof(EchoRequest)));
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_ReturnsFailure_WhenBehaviorThrowsException()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(
            loggerCollector: loggerCollector,
            configuration: cfg => cfg.AddPipelineBehavior(typeof(ThrowingPipelineBehavior<,>)));

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoRequest("boom"), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.exception");
        error.Message.ShouldContain("Error running");
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("mediator.behavior.exception")
            && entry.Message.Contains("ThrowingPipelineBehavior"));
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_ReturnsFailure_WhenPreProcessorThrows()
    {
        var services = BuildServiceCollection(configuration: cfg => cfg.AddRequestPreProcessor(typeof(ThrowingEchoPreProcessor)));

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoRequest("value"), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.preprocessor.exception");
        error.Message.ShouldContain("Error running");
    }

    [Fact]
    public async Task Send_ReturnsFailure_WhenPreProcessorCancels()
    {
        var services = BuildServiceCollection(configuration: cfg => cfg.AddRequestPreProcessor(typeof(CancellingEchoPreProcessor)));

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Send(new EchoRequest("value"), cts.Token);

        var error = ExpectFailure(result, "mediator.preprocessor.cancelled");
        error.Message.ShouldContain("cancelled");
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_ReturnsFailure_WhenPostProcessorThrows()
    {
        var services = BuildServiceCollection(configuration: cfg => cfg.AddRequestPostProcessor(typeof(ThrowingEchoPostProcessor)));

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoRequest("value"), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.postprocessor.exception");
        error.Message.ShouldContain("Error running");
    }

    [Fact]
    public async Task Send_ReturnsFailure_WhenPostProcessorCancels()
    {
        var services = BuildServiceCollection(configuration: cfg => cfg.AddRequestPostProcessor(typeof(CancellingEchoPostProcessor)));

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await mediator.Send(new EchoRequest("value"), cts.Token);

        var error = ExpectFailure(result, "mediator.postprocessor.cancelled");
        error.Message.ShouldContain("cancelled");
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task Send_TreatsPostProcessorCancellationWithoutTokenAsException()
    {
        var services = BuildServiceCollection();
        services.AddScoped<IRequestPostProcessor<EchoRequest, string>, AccidentallyCancellingPostProcessor>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoRequest("value"), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.postprocessor.exception");
        ExtractException(error).ShouldBeOfType<OperationCanceledException>();
        error.Message.ShouldContain(nameof(AccidentallyCancellingPostProcessor));
        error.Message.ShouldContain(nameof(EchoRequest));
    }

    [Fact(Skip = "Pure ROP: exceptions now propagate (fail-fast)")]
    public async Task ExecutePostProcessorAsync_ReturnsExceptionWhenTokenNotCancelled()
    {
        var method = typeof(Encina)
            .GetMethod("ExecutePostProcessorAsync", BindingFlags.NonPublic | BindingFlags.Static)!.
            MakeGenericMethod(typeof(EchoRequest), typeof(string));

        var postProcessor = new AccidentallyCancellingPostProcessor();
        var response = Right<MediatorError, string>("ok");

        var invocation = (Task<Option<MediatorError>>)method.Invoke(null, new object[]
        {
            postProcessor,
            new EchoRequest("request"),
            RequestContext.Create(),
            response,
            CancellationToken.None
        })!;

        var failure = await invocation;

        failure.IsSome.ShouldBeTrue();
        var error = failure.Match(err => err, () => MediatorErrors.Unknown);
        error.GetMediatorCode().ShouldBe("mediator.postprocessor.exception");
        error.Message.ShouldContain(nameof(AccidentallyCancellingPostProcessor));
        error.Message.ShouldContain(nameof(EchoRequest));
    }

    [Fact]
    public async Task Publish_LogsErrorWhenHandlerReturnsInvalidType()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        services.RemoveAll(typeof(INotificationHandler<SampleNotification>));
        services.AddScoped<INotificationHandler<SampleNotification>, MisleadingNotificationHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Publish(new SampleNotification(11), CancellationToken.None);

        // In pure ROP, invalid return type is detected during validation
        var error = ExpectFailure(result, "mediator.notification.invalid_return");
        error.Message.ShouldContain(nameof(MisleadingNotificationHandler));
        loggerCollector.Entries.ShouldContain(entry =>
            entry.LogLevel == LogLevel.Error
            && entry.Message.Contains("Error while publishing notification")
            && entry.Message.Contains(nameof(MisleadingNotificationHandler)));
    }

    [Fact]
    public async Task Send_DoesNotCaptureSynchronizationContext()
    {
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EncinaTests).Assembly);
        services.RemoveAll(typeof(IRequestHandler<AsyncRequest, string>));
        services.AddScoped<IRequestHandler<AsyncRequest, string>, AsyncRequestHandler>();
        services.RemoveAll(typeof(IPipelineBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TrackingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondTrackingBehavior<,>));
        services.AddScoped<PipelineTracker>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var context = new RecordingSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var request = new AsyncRequest("value");
#pragma warning disable xUnit1030
            var result = await mediator.Send(request, CancellationToken.None).ConfigureAwait(false);
#pragma warning restore xUnit1030
            var response = ExpectSuccess(result);
            response.ShouldBe("value:async");
            context.PostCallCount.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    private static readonly string[] expectedAsyncNotification = new[] { "Async:10" };

    [Fact]
    public async Task Publish_DoesNotCaptureSynchronizationContext()
    {
        var tracker = new NotificationTracker();
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EncinaTests).Assembly);
        services.RemoveAll(typeof(INotificationHandler<SampleNotification>));
        services.AddScoped(_ => tracker);
        services.AddScoped<INotificationHandler<SampleNotification>, AsyncSampleNotificationHandler>();
        services.AddScoped<PipelineTracker>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var context = new RecordingSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
#pragma warning disable xUnit1030
            var publishResult = await mediator.Publish(new SampleNotification(10), CancellationToken.None).ConfigureAwait(false);
#pragma warning restore xUnit1030
            ExpectSuccess(publishResult);
            tracker.Handled.ShouldBe(expectedAsyncNotification);
            context.PostCallCount.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task Send_PipelineStages_DoNotCaptureSynchronizationContext()
    {
        var tracker = new AsyncPipelineTracker();
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EncinaTests).Assembly);
        services.RemoveAll(typeof(IRequestHandler<AsyncPipelineRequest, string>));
        services.AddScoped(_ => tracker);
        services.AddScoped<IRequestPreProcessor<AsyncPipelineRequest>, AsyncPreProcessor>();
        services.AddScoped<IRequestHandler<AsyncPipelineRequest, string>, AsyncPipelineRequestHandler>();
        services.AddScoped<IRequestPostProcessor<AsyncPipelineRequest, string>, AsyncPostProcessor>();
        services.RemoveAll(typeof(IPipelineBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TrackingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondTrackingBehavior<,>));
        services.AddScoped<PipelineTracker>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var context = new RecordingSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
#pragma warning disable xUnit1030
            var result = await mediator.Send(new AsyncPipelineRequest("stage"), CancellationToken.None).ConfigureAwait(false);
#pragma warning restore xUnit1030
            var response = ExpectSuccess(result);
            response.ShouldBe("stage:async");
            tracker.Events.ShouldContain("pre");
            tracker.Events.ShouldContain("handler");
            tracker.Events.ShouldContain("post");
            tracker.Events.IndexOf("pre").ShouldBeLessThan(tracker.Events.IndexOf("handler"));
            tracker.Events.LastIndexOf("handler").ShouldBeLessThan(tracker.Events.LastIndexOf("post"));
            context.PostCallCount.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task Send_PipelineStages_DoNotPostToSynchronizationContext()
    {
        var tracker = new AsyncPipelineTracker();
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EncinaTests).Assembly);
        services.RemoveAll(typeof(IRequestHandler<AsyncPipelineRequest, string>));
        services.AddScoped(_ => tracker);
        services.AddScoped<IRequestPreProcessor<AsyncPipelineRequest>, AsyncPreProcessor>();
        services.AddScoped<IRequestHandler<AsyncPipelineRequest, string>, AsyncPipelineRequestHandler>();
        services.AddScoped<IRequestPostProcessor<AsyncPipelineRequest, string>, AsyncPostProcessor>();
        services.RemoveAll(typeof(IPipelineBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TrackingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondTrackingBehavior<,>));
        services.AddScoped<PipelineTracker>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var context = new RecordingSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
#pragma warning disable xUnit1030
            var result = await mediator.Send(new AsyncPipelineRequest("stage"), CancellationToken.None).ConfigureAwait(false);
#pragma warning restore xUnit1030
            var response = ExpectSuccess(result);
            response.ShouldBe("stage:async");
            context.PostCallCount.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    private static ServiceCollection BuildServiceCollection(
        PipelineTracker? pipelineTracker = null,
        NotificationTracker? notificationTracker = null,
        LoggerCollector? loggerCollector = null,
        Action<EncinaConfiguration>? configuration = null,
        IMediatorMetrics? mediatorMetrics = null)
    {
        var services = new ServiceCollection();
        services.AddEncina(cfg => configuration?.Invoke(cfg));

        services.AddScoped<IRequestHandler<EchoRequest, string>, EchoRequestHandler>();
        services.AddScoped<IRequestHandler<FaultyRequest, string>, FaultyRequestHandler>();
        services.AddScoped<IRequestHandler<CancellableRequest, string>, CancellableRequestHandler>();
        services.AddScoped<IRequestHandler<NullTaskRequest, string>, NullTaskRequestHandler>();
        services.AddScoped<IRequestHandler<AccidentalCancellationRequest, string>, AccidentalCancellationRequestHandler>();
        services.AddScoped<IRequestHandler<LifecycleRequest, string>, LifecycleRequestHandler>();
        services.AddScoped<IRequestHandler<AsyncRequest, string>, AsyncRequestHandler>();
        services.AddScoped<IRequestHandler<AsyncPipelineRequest, string>, AsyncPipelineRequestHandler>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TrackingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondTrackingBehavior<,>));

        services.AddScoped<IRequestPreProcessor<LifecycleRequest>, LifecyclePreProcessor>();
        services.AddScoped<IRequestPostProcessor<LifecycleRequest, string>, LifecyclePostProcessor>();
        services.AddScoped<IRequestPreProcessor<AsyncPipelineRequest>, AsyncPreProcessor>();
        services.AddScoped<IRequestPostProcessor<AsyncPipelineRequest, string>, AsyncPostProcessor>();

        services.AddScoped<INotificationHandler<SampleNotification>, FirstSampleNotificationHandler>();
        services.AddScoped<INotificationHandler<SampleNotification>, SecondSampleNotificationHandler>();

        services.AddScoped(_ => pipelineTracker ?? new PipelineTracker());
        services.AddScoped(_ => notificationTracker ?? new NotificationTracker());

        if (loggerCollector is not null)
        {
            services.AddSingleton(loggerCollector);
            services.AddSingleton<ILogger<global::Encina.Encina>>(sp => new ListLogger<global::Encina.Encina>(sp.GetRequiredService<LoggerCollector>()));
        }

        if (mediatorMetrics is not null)
        {
            services.RemoveAll(typeof(IMediatorMetrics));
            services.AddSingleton(_ => mediatorMetrics);
        }

        return services;
    }

    // Supporting types ------------------------------------------------------

    private sealed record EchoRequest(string Value) : IRequest<string>;

    private sealed class EchoRequestHandler(EncinaTests.PipelineTracker tracker) : IRequestHandler<EchoRequest, string>
    {
        private readonly PipelineTracker _tracker = tracker;

        public Task<Either<MediatorError, string>> Handle(EchoRequest request, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("handler");
            return Task.FromResult(Right<MediatorError, string>(request.Value));
        }
    }

    private sealed record FaultyRequest() : IRequest<string>;

    private sealed class FaultyRequestHandler : IRequestHandler<FaultyRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(FaultyRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }

    private sealed record CancellableRequest() : IRequest<string>;

    private sealed class CancellableRequestHandler : IRequestHandler<CancellableRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(CancellableRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Right<MediatorError, string>("ok"));
        }
    }

    private sealed record NullTaskRequest(string Value) : IRequest<string>;

    private sealed class NullTaskRequestHandler : IRequestHandler<NullTaskRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(NullTaskRequest request, CancellationToken cancellationToken)
            => null!;
    }

    private sealed record CacheProbeRequest(string Value) : IRequest<string>;

    private sealed class CacheProbeRequestHandler : IRequestHandler<CacheProbeRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(CacheProbeRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Right<MediatorError, string>(request.Value + ":cached"));
    }

    private sealed record AccidentalCancellationRequest() : IRequest<string>;

    private sealed class AccidentalCancellationRequestHandler : IRequestHandler<AccidentalCancellationRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(AccidentalCancellationRequest request, CancellationToken cancellationToken)
            => throw new OperationCanceledException("forced cancellation without linked token");
    }

    private sealed record CancelledOutcomeRequest() : IRequest<string>;

    private sealed class CancelledOutcomeRequestHandler : IRequestHandler<CancelledOutcomeRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(CancelledOutcomeRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Right<MediatorError, string>("ok"));
    }

    private sealed class CancelledOutcomeBehavior : IPipelineBehavior<CancelledOutcomeRequest, string>
    {
        public ValueTask<Either<MediatorError, string>> Handle(CancelledOutcomeRequest request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => ValueTask.FromResult(Left<MediatorError, string>(MediatorErrors.Create("cancelled", "Explicit cancellation.")));
    }

    private sealed record LifecycleRequest(string Value) : IRequest<string>;

    private sealed class LifecycleRequestHandler(EncinaTests.LifecycleTracker tracker) : IRequestHandler<LifecycleRequest, string>
    {
        private readonly LifecycleTracker _tracker = tracker;

        public Task<Either<MediatorError, string>> Handle(LifecycleRequest request, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("handler");
            return Task.FromResult(Right<MediatorError, string>(request.Value + ":ok"));
        }
    }

    private sealed record SampleNotification(int Value) : INotification;

    private sealed record UnhandledNotification() : INotification;

    private sealed record AccidentalCancellationNotification() : INotification;

    private sealed record ExplicitNotification() : INotification;

    private sealed class FirstSampleNotificationHandler(EncinaTests.NotificationTracker tracker) : INotificationHandler<SampleNotification>
    {
        private readonly NotificationTracker _tracker = tracker;

        public Task<Either<MediatorError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _tracker.Handled.Add($"A:{notification.Value}");
            return Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
        }
    }

    private sealed class SecondSampleNotificationHandler(EncinaTests.NotificationTracker tracker) : INotificationHandler<SampleNotification>
    {
        private readonly NotificationTracker _tracker = tracker;

        public Task<Either<MediatorError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _tracker.Handled.Add($"B:{notification.Value}");
            return Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
        }
    }

    private sealed class FaultyNotificationHandler : INotificationHandler<SampleNotification>
    {
        public Task<Either<MediatorError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
            => throw new InvalidOperationException("notify-failure");
    }

    private sealed class NullReturningNotificationHandler : INotificationHandler<SampleNotification>
    {
        public Task<Either<MediatorError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
            => null!;
    }

    private sealed class CancellableNotificationHandler : INotificationHandler<SampleNotification>
    {
        public Task<Either<MediatorError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
        }
    }

    private sealed class AccidentalCancellationNotificationHandler : INotificationHandler<AccidentalCancellationNotification>
    {
        public Task<Either<MediatorError, Unit>> Handle(AccidentalCancellationNotification notification, CancellationToken cancellationToken)
            => throw new OperationCanceledException("forced cancellation without linked token");
    }

    private sealed class ExplicitInterfaceNotificationHandler : INotificationHandler<ExplicitNotification>
    {
        Task<Either<MediatorError, Unit>> INotificationHandler<ExplicitNotification>.Handle(ExplicitNotification notification, CancellationToken cancellationToken)
            => Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
    }

    private sealed class ThrowingExplicitNotificationHandler
    {
        public static Task<Either<MediatorError, Unit>> Handle(ExplicitNotification notification, CancellationToken cancellationToken)
            => throw new InvalidOperationException("explicit boom");
    }

    private sealed class InvalidNotificationResultHandler
    {
        public static string Handle(ExplicitNotification notification, CancellationToken cancellationToken)
            => "invalid";
    }

    private sealed class ThrowingCancelledExplicitNotificationHandler
    {
        public static Task<Either<MediatorError, Unit>> Handle(ExplicitNotification notification, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("explicit cancellation", cancellationToken);
        }
    }

    private sealed class TaskCancellingExplicitNotificationHandler
    {
        public static Task<Either<MediatorError, Unit>> Handle(ExplicitNotification notification, CancellationToken cancellationToken)
            => Task.FromCanceled<Either<MediatorError, Unit>>(cancellationToken);
    }

    private sealed record PostProcessorRequest(string Value) : IRequest<string>;

    private sealed class PostProcessorRequestHandler(EncinaTests.PostProcessorTracker tracker) : IRequestHandler<PostProcessorRequest, string>
    {
        private readonly PostProcessorTracker _tracker = tracker;

        public Task<Either<MediatorError, string>> Handle(PostProcessorRequest request, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("handler");
            return Task.FromResult(Right<MediatorError, string>(request.Value + ":handled"));
        }
    }

    private sealed class RecordingPostProcessor(EncinaTests.PostProcessorTracker tracker) : IRequestPostProcessor<PostProcessorRequest, string>
    {
        private readonly PostProcessorTracker _tracker = tracker;

        public Task Process(PostProcessorRequest request, IRequestContext context, Either<MediatorError, string> response, CancellationToken cancellationToken)
        {
            response.IfRight(value => _tracker.Events.Add($"post:{value}"));
            response.IfLeft(_ => _tracker.Events.Add("post:error"));
            return Task.CompletedTask;
        }
    }

    private sealed class PostProcessorTracker
    {
        public List<string> Events { get; } = new();
    }

    private sealed class AccidentallyCancellingPostProcessor : IRequestPostProcessor<EchoRequest, string>
    {
        public Task Process(EchoRequest request, IRequestContext context, Either<MediatorError, string> response, CancellationToken cancellationToken)
            => throw new OperationCanceledException("accidental cancellation");
    }

    private sealed class CancellingExplicitNotificationHandler : INotificationHandler<ExplicitNotification>
    {
        public Task<Either<MediatorError, Unit>> Handle(ExplicitNotification notification, CancellationToken cancellationToken)
            => throw new OperationCanceledException("explicit cancellation", cancellationToken);
    }

    private sealed class MissingCancellationTokenNotificationHandler
    {
        public static Task<Either<MediatorError, Unit>> Handle(ExplicitNotification notification)
            => Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
    }

    private sealed class AsyncSampleNotificationHandler(EncinaTests.NotificationTracker tracker) : INotificationHandler<SampleNotification>
    {
        private readonly NotificationTracker _tracker = tracker;

        public async Task<Either<MediatorError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            _tracker.Handled.Add($"Async:{notification.Value}");
            return Right<MediatorError, Unit>(Unit.Default);
        }
    }

    private sealed record MissingHandlerRequest : IRequest<int>;

    private sealed record AsyncRequest(string Value) : IRequest<string>;

    private sealed class AsyncRequestHandler : IRequestHandler<AsyncRequest, string>
    {
        public async Task<Either<MediatorError, string>> Handle(AsyncRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return Right<MediatorError, string>(request.Value + ":async");
        }
    }

    private sealed class TrackingBehavior<TRequest, TResponse>(EncinaTests.PipelineTracker tracker) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly PipelineTracker _tracker = tracker;

        public async ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("tracking:before");
            var response = await nextStep().ConfigureAwait(false);
            _tracker.Events.Add("tracking:after");
            return response;
        }
    }

    private sealed class SecondTrackingBehavior<TRequest, TResponse>(EncinaTests.PipelineTracker tracker) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly PipelineTracker _tracker = tracker;

        public async ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("second:before");
            var response = await nextStep().ConfigureAwait(false);
            _tracker.Events.Add("second:after");
            return response;
        }
    }

    private sealed class LifecyclePreProcessor(EncinaTests.LifecycleTracker tracker) : IRequestPreProcessor<LifecycleRequest>
    {
        private readonly LifecycleTracker _tracker = tracker;

        public Task Process(LifecycleRequest request, IRequestContext context, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("pre");
            return Task.CompletedTask;
        }
    }

    private sealed class LifecyclePostProcessor(EncinaTests.LifecycleTracker tracker) : IRequestPostProcessor<LifecycleRequest, string>
    {
        private readonly LifecycleTracker _tracker = tracker;

        public Task Process(LifecycleRequest request, IRequestContext context, Either<MediatorError, string> response, CancellationToken cancellationToken)
        {
            if (response.IsRight)
            {
                _tracker.Events.Add("post");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class LifecycleTracker
    {
        public List<string> Events { get; } = new();
    }

    private sealed class PipelineTracker
    {
        public List<string> Events { get; } = new();
    }

    private sealed class NotificationTracker
    {
        public List<string> Handled { get; } = new();
    }

    private sealed class AsyncPipelineTracker
    {
        public List<string> Events { get; } = new();
    }

    private sealed class LoggerCollector
    {
        public ConcurrentBag<LogEntry> Entries { get; } = new();
    }

    private sealed class MediatorMetricsSpy : IMediatorMetrics
    {
        public List<(string Kind, string Name, TimeSpan Duration)> Successes { get; } = new();
        public List<(string Kind, string Name, TimeSpan Duration, string Reason)> Failures { get; } = new();

        public void TrackSuccess(string requestKind, string requestName, TimeSpan duration)
            => Successes.Add((requestKind, requestName, duration));

        public void TrackFailure(string requestKind, string requestName, TimeSpan duration, string reason)
            => Failures.Add((requestKind, requestName, duration, reason));
    }

    private sealed class CancellingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => ValueTask.FromCanceled<Either<MediatorError, TResponse>>(cancellationToken);
    }

    private sealed class ThrowingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => throw new InvalidOperationException($"behavior failure for {typeof(TRequest).Name}");
    }

    private sealed class ThrowingEchoPreProcessor : IRequestPreProcessor<EchoRequest>
    {
        public Task Process(EchoRequest request, IRequestContext context, CancellationToken cancellationToken)
            => throw new InvalidOperationException("pre failure");
    }

    private sealed class CancellingEchoPreProcessor : IRequestPreProcessor<EchoRequest>
    {
        public Task Process(EchoRequest request, IRequestContext context, CancellationToken cancellationToken)
            => Task.FromCanceled(cancellationToken);
    }

    private sealed class ThrowingEchoPostProcessor : IRequestPostProcessor<EchoRequest, string>
    {
        public Task Process(EchoRequest request, IRequestContext context, Either<MediatorError, string> response, CancellationToken cancellationToken)
            => throw new InvalidOperationException("post failure");
    }

    private sealed class CancellingEchoPostProcessor : IRequestPostProcessor<EchoRequest, string>
    {
        public Task Process(EchoRequest request, IRequestContext context, Either<MediatorError, string> response, CancellationToken cancellationToken)
            => Task.FromCanceled(cancellationToken);
    }

    private sealed class MisleadingNotificationHandler : INotificationHandler<SampleNotification>
    {
        Task<Either<MediatorError, Unit>> INotificationHandler<SampleNotification>.Handle(SampleNotification notification, CancellationToken cancellationToken)
            => Task.FromResult(Right<MediatorError, Unit>(Unit.Default));

        public static string Handle(SampleNotification notification, CancellationToken cancellationToken)
            => "invalid";
    }

    private static T ExpectSuccess<T>(Either<MediatorError, T> result)
    {
        var failureMessage = result.Match(
            Left: err => $"Expected success but got a failure: {err.GetMediatorCode()} - {err.Message}",
            Right: _ => string.Empty);

        result.IsRight.ShouldBeTrue(failureMessage);

        return result.Match(
            Left: _ => throw new InvalidOperationException("ExpectSuccess received an Either in a failure state."),
            Right: value => value!);
    }

    private static MediatorError ExpectFailure<T>(Either<MediatorError, T> result, string expectedCode)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(
            Left: err => err,
            Right: _ => MediatorErrors.Unknown);
        error.GetMediatorCode().ShouldBe(expectedCode);
        return error;
    }

    private static Exception ExtractException(MediatorError error)
        => error.Exception.Match(
            Some: ex => ex,
            None: () => throw new InvalidOperationException("Expected the error to carry an exception."));

    private static Func<object, TNotification, CancellationToken, Task<Either<MediatorError, Unit>>> CreateInvokeNotificationDelegate<TNotification>()
    {
        var method = typeof(Encina)
            .GetMethod("InvokeNotificationHandler", BindingFlags.NonPublic | BindingFlags.Static)!.
            MakeGenericMethod(typeof(TNotification));
        return (Func<object, TNotification, CancellationToken, Task<Either<MediatorError, Unit>>>)method.CreateDelegate(
            typeof(Func<object, TNotification, CancellationToken, Task<Either<MediatorError, Unit>>>));
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message, Exception? Exception);

    private sealed class ListLogger<T>(EncinaTests.LoggerCollector collector) : ILogger<T>
    {
        private readonly LoggerCollector _collector = collector;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
            {
                return;
            }

            _collector.Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        IDisposable ILogger.BeginScope<TState>(TState state) => NoopScope.Instance;

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class ActivityCollector : IDisposable
    {
        private readonly ActivityListener _listener;

        public ActivityCollector()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, "Encina", StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => Activities.Add(activity)
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public List<Activity> Activities { get; } = new();

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed record AsyncPipelineRequest(string Value) : IRequest<string>;

    private sealed class AsyncPreProcessor(EncinaTests.AsyncPipelineTracker tracker) : IRequestPreProcessor<AsyncPipelineRequest>
    {
        private readonly AsyncPipelineTracker _tracker = tracker;

        public async Task Process(AsyncPipelineRequest request, IRequestContext context, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            _tracker.Events.Add("pre");
        }
    }

    private sealed class AsyncPipelineRequestHandler(EncinaTests.AsyncPipelineTracker tracker) : IRequestHandler<AsyncPipelineRequest, string>
    {
        private readonly AsyncPipelineTracker _tracker = tracker;

        public async Task<Either<MediatorError, string>> Handle(AsyncPipelineRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            _tracker.Events.Add("handler");
            return Right<MediatorError, string>(request.Value + ":async");
        }
    }

    private sealed class AsyncPostProcessor(EncinaTests.AsyncPipelineTracker tracker) : IRequestPostProcessor<AsyncPipelineRequest, string>
    {
        private readonly AsyncPipelineTracker _tracker = tracker;

        public async Task Process(AsyncPipelineRequest request, IRequestContext context, Either<MediatorError, string> response, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            if (response.IsRight)
            {
                _tracker.Events.Add("post");
            }
        }
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext, IDisposable
    {
        private int _postCallCount;

        public int PostCallCount => Volatile.Read(ref _postCallCount);

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCallCount);
            ThreadPool.QueueUserWorkItem(static tuple =>
            {
                var (callback, callbackState) = ((SendOrPostCallback, object?))tuple!;
                callback(callbackState);
            }, (d, state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCallCount);
            d(state);
        }

        public void Dispose()
        {
            // No resources to clean up; interface implemented for using pattern symmetry.
        }
    }

    [Fact]
    public void IsCancellationCode_ReturnsFalse_WhenCodeIsNull()
    {
        var method = typeof(Encina).GetMethod("IsCancellationCode", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [null!])!;
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCancellationCode_ReturnsFalse_WhenCodeIsEmpty()
    {
        var method = typeof(Encina).GetMethod("IsCancellationCode", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [""])!;
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCancellationCode_ReturnsFalse_WhenCodeIsWhitespace()
    {
        var method = typeof(Encina).GetMethod("IsCancellationCode", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (bool)method!.Invoke(null, ["   "])!;
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCancellationCode_ReturnsTrue_WhenCodeContainsCancelled()
    {
        var method = typeof(Encina).GetMethod("IsCancellationCode", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (bool)method!.Invoke(null, ["operation.cancelled"])!;
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsCancellationCode_ReturnsTrue_WhenCodeContainsCancelledUpperCase()
    {
        var method = typeof(Encina).GetMethod("IsCancellationCode", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (bool)method!.Invoke(null, ["OPERATION.CANCELLED"])!;
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsCancellationCode_ReturnsFalse_WhenCodeDoesNotContainCancelled()
    {
        var method = typeof(Encina).GetMethod("IsCancellationCode", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (bool)method!.Invoke(null, ["operation.failed"])!;
        result.ShouldBeFalse();
    }

}
