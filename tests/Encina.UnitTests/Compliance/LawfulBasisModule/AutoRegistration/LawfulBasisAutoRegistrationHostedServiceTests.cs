using System.Reflection;

using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.AutoRegistration;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

using static LanguageExt.Prelude;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

#pragma warning disable CA2012 // ValueTask

namespace Encina.UnitTests.Compliance.LawfulBasisModule.AutoRegistration;

/// <summary>
/// Unit tests for <see cref="LawfulBasisAutoRegistrationHostedService"/>.
/// Since the type is internal, these tests exercise it via reflection-based instantiation.
/// </summary>
public class LawfulBasisAutoRegistrationHostedServiceTests
{
    [LawfulBasis(GDPRLawfulBasis.Contract, Purpose = "Test")]
    public sealed record DecoratedCommand;

    public sealed record UndecoratedCommand;

    private readonly ILawfulBasisService _service = Substitute.For<ILawfulBasisService>();

    private static readonly Type HostedServiceType =
        typeof(LawfulBasisAutoRegistrationDescriptor).Assembly
            .GetType("Encina.Compliance.LawfulBasis.AutoRegistration.LawfulBasisAutoRegistrationHostedService")!;

    private object CreateHostedService(LawfulBasisAutoRegistrationDescriptor descriptor)
    {
        var nullLoggerGeneric = typeof(NullLogger<>).MakeGenericType(HostedServiceType);
        // NullLogger<T>.Instance is a static field, not a property
        var instanceField = nullLoggerGeneric.GetField("Instance",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Could not find NullLogger<T>.Instance field");
        var loggerInstance = instanceField.GetValue(null)
            ?? throw new InvalidOperationException("NullLogger<T>.Instance returned null");

        var ctor = HostedServiceType.GetConstructors().Single();
        return ctor.Invoke(new object?[] { _service, descriptor, loggerInstance });
    }

    private static Task InvokeStartAsync(object hostedService)
    {
        var method = HostedServiceType.GetMethod("StartAsync", BindingFlags.Public | BindingFlags.Instance)!;
        return (Task)method.Invoke(hostedService, new object?[] { CancellationToken.None })!;
    }

    private static Task InvokeStopAsync(object hostedService)
    {
        var method = HostedServiceType.GetMethod("StopAsync", BindingFlags.Public | BindingFlags.Instance)!;
        return (Task)method.Invoke(hostedService, new object?[] { CancellationToken.None })!;
    }

    [Fact]
    public async Task StartAsync_EmptyAssemblies_CompletesWithoutErrors()
    {
        var descriptor = new LawfulBasisAutoRegistrationDescriptor(
            new List<Assembly>(),
            new Dictionary<Type, GDPRLawfulBasis>());
        var sut = CreateHostedService(descriptor);

        await InvokeStartAsync(sut);

        await _service.DidNotReceiveWithAnyArgs().RegisterAsync(
            default, default!, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task StartAsync_WithAssembly_RegistersDecoratedTypes()
    {
        _service.RegisterAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<GDPRLawfulBasis>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Guid>>(Right<EncinaError, Guid>(Guid.NewGuid())));

        var descriptor = new LawfulBasisAutoRegistrationDescriptor(
            new[] { typeof(LawfulBasisAutoRegistrationHostedServiceTests).Assembly },
            new Dictionary<Type, GDPRLawfulBasis>());
        var sut = CreateHostedService(descriptor);

        await InvokeStartAsync(sut);

        // Should have at least one registration (DecoratedCommand has the attribute)
        await _service.ReceivedWithAnyArgs().RegisterAsync(
            default, default!, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task StartAsync_WithDefaultBases_RegistersEach()
    {
        _service.RegisterAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<GDPRLawfulBasis>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Guid>>(Right<EncinaError, Guid>(Guid.NewGuid())));

        var defaults = new Dictionary<Type, GDPRLawfulBasis>
        {
            [typeof(UndecoratedCommand)] = GDPRLawfulBasis.Contract
        };
        var descriptor = new LawfulBasisAutoRegistrationDescriptor(
            new List<Assembly>(),
            defaults);
        var sut = CreateHostedService(descriptor);

        await InvokeStartAsync(sut);

        await _service.ReceivedWithAnyArgs().RegisterAsync(
            default, default!, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutErrors()
    {
        var descriptor = new LawfulBasisAutoRegistrationDescriptor(
            new List<Assembly>(),
            new Dictionary<Type, GDPRLawfulBasis>());
        var sut = CreateHostedService(descriptor);

        await InvokeStopAsync(sut);
    }

    [Fact]
    public void Descriptor_StoresAssembliesAndDefaults()
    {
        var assemblies = new[] { typeof(object).Assembly };
        var defaults = new Dictionary<Type, GDPRLawfulBasis>
        {
            [typeof(object)] = GDPRLawfulBasis.PublicTask
        };
        var descriptor = new LawfulBasisAutoRegistrationDescriptor(assemblies, defaults);

        descriptor.Assemblies.ShouldBe(assemblies);
        descriptor.DefaultBases.ShouldBe(defaults);
    }
}
