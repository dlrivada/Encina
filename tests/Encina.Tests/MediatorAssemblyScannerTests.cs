using System.Reflection;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests;

public sealed class MediatorAssemblyScannerTests
{
    [Fact]
    public void GetRegistrations_ThrowsWhenAssemblyIsNull()
    {
        ResetScannerCache();
        var exception = Should.Throw<ArgumentNullException>(() => MediatorAssemblyScanner.GetRegistrations(null!));
        exception.ParamName.ShouldBe("assembly");
    }

    [Fact]
    public void GetRegistrations_IncludesHandlersNotificationsAndProcessors()
    {
        ResetScannerCache();
        var assembly = typeof(MediatorAssemblyScannerTests).Assembly;

        var result = MediatorAssemblyScanner.GetRegistrations(assembly);

        result.HandlerRegistrations.ShouldContain(r => r.ServiceType == typeof(IRequestHandler<PingRequest, string>) && r.ImplementationType == typeof(PingHandler));
        result.NotificationRegistrations.ShouldContain(r => r.ServiceType == typeof(INotificationHandler<PingNotification>) && r.ImplementationType == typeof(PingNotificationHandler));
        result.PipelineRegistrations.ShouldContain(r => r.ServiceType == typeof(IPipelineBehavior<PingRequest, string>) && r.ImplementationType == typeof(ClosedPipelineBehavior));
        result.RequestPreProcessorRegistrations.ShouldContain(r => r.ServiceType == typeof(IRequestPreProcessor<PingRequest>) && r.ImplementationType == typeof(ClosedPreProcessor));
        result.RequestPostProcessorRegistrations.ShouldContain(r => r.ServiceType == typeof(IRequestPostProcessor<PingRequest, string>) && r.ImplementationType == typeof(ClosedPostProcessor));
    }

    [Fact]
    public void GetRegistrations_UsesOpenGenericServiceTypesWhenImplementationIsOpenGeneric()
    {
        ResetScannerCache();
        var assembly = typeof(MediatorAssemblyScannerTests).Assembly;

        var result = MediatorAssemblyScanner.GetRegistrations(assembly);

        result.PipelineRegistrations.ShouldContain(r => r.ServiceType == typeof(IPipelineBehavior<,>) && r.ImplementationType == typeof(OpenGenericPipelineBehavior<,>));
        result.RequestPreProcessorRegistrations.ShouldContain(r => r.ServiceType == typeof(IRequestPreProcessor<>) && r.ImplementationType == typeof(OpenGenericPreProcessor<>));
        result.RequestPostProcessorRegistrations.ShouldContain(r => r.ServiceType == typeof(IRequestPostProcessor<,>) && r.ImplementationType == typeof(OpenGenericPostProcessor<,>));
    }

    [Fact]
    public void GetRegistrations_IgnoresTypesThatDoNotImplementMediatorContracts()
    {
        ResetScannerCache();
        var assembly = typeof(MediatorAssemblyScannerTests).Assembly;

        var result = MediatorAssemblyScanner.GetRegistrations(assembly);

        result.HandlerRegistrations.ShouldNotContain(r => r.ImplementationType == typeof(UnrelatedGenericType));
        result.NotificationRegistrations.ShouldNotContain(r => r.ImplementationType == typeof(UnrelatedGenericType));
        result.PipelineRegistrations.ShouldNotContain(r => r.ImplementationType == typeof(UnrelatedGenericType));
        result.RequestPreProcessorRegistrations.ShouldNotContain(r => r.ImplementationType == typeof(UnrelatedGenericType));
        result.RequestPostProcessorRegistrations.ShouldNotContain(r => r.ImplementationType == typeof(UnrelatedGenericType));
    }

    [Fact]
    public void GetRegistrations_IgnoresAbstractMediatorTypes()
    {
        ResetScannerCache();
        var assembly = typeof(MediatorAssemblyScannerTests).Assembly;

        var result = MediatorAssemblyScanner.GetRegistrations(assembly);

        result.HandlerRegistrations.ShouldNotContain(r => r.ImplementationType == typeof(AbstractHandler));
        result.PipelineRegistrations.ShouldNotContain(r => r.ImplementationType == typeof(AbstractPipelineBehavior));
    }

    [Fact]
    public void GetRegistrations_IgnoresValueTypeMediatorImplementations()
    {
        ResetScannerCache();
        var assembly = typeof(MediatorAssemblyScannerTests).Assembly;

        var result = MediatorAssemblyScanner.GetRegistrations(assembly);

        result.HandlerRegistrations.ShouldNotContain(r => r.ImplementationType == typeof(ValueTypeHandler));
    }

    private sealed record PingRequest(string Value) : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<PingRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(PingRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Right<MediatorError, string>(request.Value));
    }

    private sealed record PingNotification(string Value) : INotification;

    private sealed class PingNotificationHandler : INotificationHandler<PingNotification>
    {
        public Task<Either<MediatorError, Unit>> Handle(PingNotification notification, CancellationToken cancellationToken)
            => Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
    }

    private sealed class ClosedPipelineBehavior : IPipelineBehavior<PingRequest, string>
    {
        public ValueTask<Either<MediatorError, string>> Handle(PingRequest request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class OpenGenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private abstract class AbstractPipelineBehavior : IPipelineBehavior<PingRequest, string>
    {
        public abstract ValueTask<Either<MediatorError, string>> Handle(PingRequest request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken);
    }

    private sealed class ClosedPreProcessor : IRequestPreProcessor<PingRequest>
    {
        public Task Process(PingRequest request, IRequestContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OpenGenericPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    {
        public Task Process(TRequest request, IRequestContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ClosedPostProcessor : IRequestPostProcessor<PingRequest, string>
    {
        public Task Process(PingRequest request, IRequestContext context, Either<MediatorError, string> response, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OpenGenericPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    {
        public Task Process(TRequest request, IRequestContext context, Either<MediatorError, TResponse> response, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class UnrelatedGenericType : IComparable<int>
    {
        public int CompareTo(int other) => 0;
    }

    private abstract class AbstractHandler : IRequestHandler<PingRequest, string>
    {
        public abstract Task<Either<MediatorError, string>> Handle(PingRequest request, CancellationToken cancellationToken);
    }

    private struct ValueTypeHandler : IRequestHandler<PingRequest, string>
    {
        public Task<Either<MediatorError, string>> Handle(PingRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Right<MediatorError, string>(request.Value));
    }

    private static void ResetScannerCache()
    {
        var cacheField = typeof(MediatorAssemblyScanner).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic);
        cacheField.ShouldNotBeNull();

        var cacheInstance = cacheField!.GetValue(null);
        cacheInstance.ShouldNotBeNull();

        var clearMethod = cacheInstance!.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
        clearMethod.ShouldNotBeNull();
        clearMethod!.Invoke(cacheInstance, null);
    }
}
