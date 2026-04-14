using Encina.gRPC;
using Encina.gRPC.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.gRPC;

/// <summary>
/// Guard tests for Encina.gRPC covering constructor and method null guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class GrpcGuardTests
{
    private static readonly ITypeResolver TypeResolver = Substitute.For<ITypeResolver>();

    // ─── GrpcEncinaService constructor guards ───

    [Fact]
    public void GrpcEncinaService_NullEncina_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(null!,
                NullLogger<GrpcEncinaService>.Instance,
                TypeResolver,
                Options.Create(new EncinaGrpcOptions())));
    }

    [Fact]
    public void GrpcEncinaService_NullLogger_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(encina, null!, TypeResolver,
                Options.Create(new EncinaGrpcOptions())));
    }

    [Fact]
    public void GrpcEncinaService_NullTypeResolver_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(encina,
                NullLogger<GrpcEncinaService>.Instance, null!,
                Options.Create(new EncinaGrpcOptions())));
    }

    [Fact]
    public void GrpcEncinaService_NullOptions_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new GrpcEncinaService(encina,
                NullLogger<GrpcEncinaService>.Instance,
                TypeResolver, null!));
    }

    [Fact]
    public void GrpcEncinaService_ValidArgs_Constructs()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GrpcEncinaService(encina,
            NullLogger<GrpcEncinaService>.Instance,
            TypeResolver,
            Options.Create(new EncinaGrpcOptions()));
        sut.ShouldNotBeNull();
    }

    // ─── GrpcEncinaService method guards ───

    [Fact]
    public async Task SendAsync_NullRequestType_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GrpcEncinaService(encina,
            NullLogger<GrpcEncinaService>.Instance,
            TypeResolver,
            Options.Create(new EncinaGrpcOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SendAsync(null!, []));
    }

    [Fact]
    public async Task SendAsync_NullRequestData_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GrpcEncinaService(encina,
            NullLogger<GrpcEncinaService>.Instance,
            TypeResolver,
            Options.Create(new EncinaGrpcOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SendAsync("SomeType", null!));
    }

    [Fact]
    public async Task PublishAsync_NullNotificationType_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GrpcEncinaService(encina,
            NullLogger<GrpcEncinaService>.Instance,
            TypeResolver,
            Options.Create(new EncinaGrpcOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.PublishAsync(null!, []));
    }

    [Fact]
    public async Task PublishAsync_NullNotificationData_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GrpcEncinaService(encina,
            NullLogger<GrpcEncinaService>.Instance,
            TypeResolver,
            Options.Create(new EncinaGrpcOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.PublishAsync("SomeType", null!));
    }

    // ─── CachingTypeResolver guards ───

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CachingTypeResolver_ResolveRequestType_InvalidName_Throws(string? name)
    {
        var sut = new CachingTypeResolver();
        Should.Throw<ArgumentException>(() => sut.ResolveRequestType(name!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CachingTypeResolver_ResolveNotificationType_InvalidName_Throws(string? name)
    {
        var sut = new CachingTypeResolver();
        Should.Throw<ArgumentException>(() => sut.ResolveNotificationType(name!));
    }

    [Fact]
    public void CachingTypeResolver_ResolveRequestType_UnknownType_ReturnsNull()
    {
        var sut = new CachingTypeResolver();
        var result = sut.ResolveRequestType("NonExistent.Type.Name");
        result.ShouldBeNull();
    }

    [Fact]
    public void CachingTypeResolver_ResolveNotificationType_UnknownType_ReturnsNull()
    {
        var sut = new CachingTypeResolver();
        var result = sut.ResolveNotificationType("NonExistent.Type.Name");
        result.ShouldBeNull();
    }

    // ─── GrpcHealthCheck ───

    [Fact]
    public void GrpcHealthCheck_Constructs()
    {
        using var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new GrpcHealthCheck(sp, null);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaGrpc_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaGrpc());
    }

    [Fact]
    public void AddEncinaGrpc_ValidServices_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IEncina>());

        var result = services.AddEncinaGrpc();

        result.ShouldNotBeNull();
        services.ShouldContain(sd => sd.ServiceType == typeof(IGrpcEncinaService));
        services.ShouldContain(sd => sd.ServiceType == typeof(ITypeResolver));
    }

    // ─── EncinaGrpcOptions ───

    [Fact]
    public void EncinaGrpcOptions_Defaults()
    {
        var options = new EncinaGrpcOptions();

        options.EnableReflection.ShouldBeTrue();
        options.EnableHealthChecks.ShouldBeTrue();
        options.MaxReceiveMessageSize.ShouldBe(4 * 1024 * 1024);
        options.MaxSendMessageSize.ShouldBe(4 * 1024 * 1024);
        options.EnableLoggingInterceptor.ShouldBeTrue();
        options.DefaultDeadline.ShouldBe(TimeSpan.FromSeconds(30));
        options.EnableCompression.ShouldBeFalse();
    }

    // ─── GrpcSerializationException ───

    [Fact]
    public void GrpcSerializationException_WithJsonException()
    {
        var inner = new System.Text.Json.JsonException("bad json");
        var ex = new GrpcSerializationException("outer", inner);
        ex.Message.ShouldBe("outer");
        ex.InnerException.ShouldBe(inner);
    }
}
