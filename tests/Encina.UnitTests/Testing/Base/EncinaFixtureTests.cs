using Encina.Testing;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Testing.Base;

public sealed class EncinaFixtureTests
{
    [Fact]
    public async Task CreateEncina_ShouldReturnWorkingInstance()
    {
        // Arrange
        var fixture = new EncinaFixture();
        var encina = fixture.CreateEncina(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });

        // Act
        var result = await encina.Send(new TestRequest("hello"));

        // Assert
        result.ShouldBeSuccess();
        var value = ExtractRight(result);
        value.ShouldBe("HELLO");
    }

    [Fact]
    public async Task CreateEncina_WithServices_ShouldRegisterCustomServices()
    {
        // Arrange
        var fixture = new EncinaFixture();
        var encina = fixture.CreateEncina(
            config =>
            {
                config.RegisterServicesFromAssemblyContaining<TestRequest>();
            },
            services =>
            {
                services.AddSingleton<ITestService, TestService>();
            });

        // Act
        var result = await encina.Send(new ServiceDependentRequest("test"));

        // Assert
        result.ShouldBeSuccess();
        var value = ExtractRight(result);
        value.ShouldBe("Service: test");
    }

    [Fact]
    public void GetRequiredService_ShouldReturnRegisteredService()
    {
        // Arrange
        var fixture = new EncinaFixture();
        fixture.CreateEncina(
            config => config.RegisterServicesFromAssemblyContaining<TestRequest>(),
            services => services.AddSingleton<ITestService, TestService>());

        // Act
        var service = fixture.GetRequiredService<ITestService>();

        // Assert
        service.ShouldNotBeNull();
        service.ShouldBeOfType<TestService>();
    }

    [Fact]
    public void GetService_ShouldReturnNullForUnregisteredService()
    {
        // Arrange
        var fixture = new EncinaFixture();
        fixture.CreateEncina(config =>
            config.RegisterServicesFromAssemblyContaining<TestRequest>());

        // Act
        var service = fixture.GetService<ITestService>();

        // Assert
        service.ShouldBeNull();
    }

    [Fact]
    public void GetRequiredService_BeforeCreateEncina_ShouldThrow()
    {
        // Arrange
        var fixture = new EncinaFixture();

        // Act & Assert
        var act = () => fixture.GetRequiredService<IEncina>();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("CreateEncina");
    }

    [Fact]
    public void ServiceProvider_BeforeCreateEncina_ShouldBeNull()
    {
        // Arrange
        var fixture = new EncinaFixture();

        // Assert
        fixture.ServiceProvider.ShouldBeNull();
    }

    [Fact]
    public void ServiceProvider_AfterCreateEncina_ShouldNotBeNull()
    {
        // Arrange
        var fixture = new EncinaFixture();
        fixture.CreateEncina(config =>
            config.RegisterServicesFromAssemblyContaining<TestRequest>());

        // Assert
        fixture.ServiceProvider.ShouldNotBeNull();
    }

    [Fact]
    public void CreateScope_ShouldReturnNewScope()
    {
        // Arrange
        var fixture = new EncinaFixture();
        fixture.CreateEncina(config =>
            config.RegisterServicesFromAssemblyContaining<TestRequest>());

        // Act
        using var scope = fixture.CreateScope();

        // Assert
        scope.ShouldNotBeNull();
        scope.ServiceProvider.ShouldNotBeNull();
    }

    [Fact]
    public void CreateScope_BeforeCreateEncina_ShouldThrow()
    {
        // Arrange
        var fixture = new EncinaFixture();

        // Act & Assert
        var act = () => fixture.CreateScope();
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("CreateEncina");
    }

    [Fact]
    public void CreateEncinaFromAssemblyContaining_ShouldWork()
    {
        // Arrange
        var fixture = new EncinaFixture();

        // Act
        var encina = fixture.CreateEncinaFromAssemblyContaining<TestRequest>();

        // Assert
        encina.ShouldNotBeNull();
    }

    #region Test Types

    private static T ExtractRight<T>(Either<EncinaError, T> either)
    {
        return either.Match(
            Right: v => v,
            Left: error => throw new InvalidOperationException($"Expected Right but got Left with error: {error}")
        );
    }

    private sealed record TestRequest(string Value) : IRequest<string>;

    private sealed class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, string>(request.Value.ToUpperInvariant()));
    }

    private interface ITestService
    {
        string Process(string value);
    }

    private sealed class TestService : ITestService
    {
        public string Process(string value) => $"Service: {value}";
    }

    private sealed record ServiceDependentRequest(string Value) : IRequest<string>;

    private sealed class ServiceDependentRequestHandler(ITestService service) : IRequestHandler<ServiceDependentRequest, string>
    {
        private readonly ITestService _service = service;

        public Task<Either<EncinaError, string>> Handle(ServiceDependentRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, string>(_service.Process(request.Value)));
    }

    #endregion
}
