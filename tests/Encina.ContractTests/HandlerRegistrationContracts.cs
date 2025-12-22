using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.ContractTests;

public sealed class HandlerRegistrationContracts
{
    [Fact]
    public void RequestHandlersAreRegisteredScopedByDefault()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(HandlerRegistrationContracts).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::Encina.IRequestHandler<SampleCommand, string>)
            && ImplementationMatches(d, typeof(SampleCommandHandler))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void RequestHandlersHonorConfiguredLifetime()
    {
        var services = new ServiceCollection();

        services.AddEncina(cfg =>
        {
            cfg.WithHandlerLifetime(ServiceLifetime.Singleton);
        }, typeof(HandlerRegistrationContracts).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::Encina.IRequestHandler<SampleCommand, string>)
            && ImplementationMatches(d, typeof(SampleCommandHandler))
            && d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void RequestHandlersAreRegisteredOnlyOnceAcrossInvocations()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(HandlerRegistrationContracts).Assembly);
        services.AddEncina(typeof(HandlerRegistrationContracts).Assembly);

        var descriptors = services
            .Where(d => d.ServiceType == typeof(global::Encina.IRequestHandler<SampleCommand, string>))
            .ToList();

        descriptors.Count.ShouldBe(1, "Request handlers should not be duplicated when registration runs multiple times.");
    }

    [Fact]
    public void NotificationHandlersAllowMultipleImplementations()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(HandlerRegistrationContracts).Assembly);

        var descriptors = services
            .Where(d => d.ServiceType == typeof(global::Encina.INotificationHandler<SampleNotification>))
            .ToList();

        descriptors.Count.ShouldBe(2, "All notification handlers should be preserved during registration.");
        descriptors.ShouldContain(d => ImplementationMatches(d, typeof(SampleNotificationHandlerOne)));
        descriptors.ShouldContain(d => ImplementationMatches(d, typeof(SampleNotificationHandlerTwo)));
    }

    private static bool ImplementationMatches(ServiceDescriptor descriptor, Type candidate)
    {
        return descriptor.ImplementationType == candidate
               || descriptor.ImplementationInstance?.GetType() == candidate;
    }

    private sealed record SampleCommand(string Payload) : global::Encina.ICommand<string>;

    private sealed class SampleCommandHandler : global::Encina.ICommandHandler<SampleCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(SampleCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>(request.Payload));
        }
    }

    private sealed record SampleNotification(string Value) : global::Encina.INotification;

    private sealed class SampleNotificationHandlerOne : global::Encina.INotificationHandler<SampleNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    private sealed class SampleNotificationHandlerTwo : global::Encina.INotificationHandler<SampleNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }
}
