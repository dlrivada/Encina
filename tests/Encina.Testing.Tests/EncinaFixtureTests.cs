using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Testing.Tests;

public sealed class EncinaFixtureTests : IDisposable
{
    private readonly EncinaFixture _fixture = new();

    [Fact]
    public async Task CreateEncina_ShouldReturnWorkingInstance()
    {
        // Arrange
        var encina = _fixture.CreateEncina(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });

        // Act
        var result = await encina.Send(new TestRequest("hello"));

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(value => value.Should().Be("HELLO"));
    }

    [Fact]
    public async Task CreateEncina_WithServices_ShouldRegisterCustomServices()
    {
        // Arrange
        var encina = _fixture.CreateEncina(
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
        result.IsRight.Should().BeTrue();
        result.IfRight(value => value.Should().Be("Service: test"));
    }

    [Fact]
    public void GetRequiredService_ShouldReturnRegisteredService()
    {
        // Arrange
        _fixture.CreateEncina(
            config => config.RegisterServicesFromAssemblyContaining<TestRequest>(),
            services => services.AddSingleton<ITestService, TestService>());

        // Act
        var service = _fixture.GetRequiredService<ITestService>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<TestService>();
    }

    [Fact]
    public void GetService_ShouldReturnNullForUnregisteredService()
    {
        // Arrange
        _fixture.CreateEncina(config =>
            config.RegisterServicesFromAssemblyContaining<TestRequest>());

        // Act
        var service = _fixture.GetService<ITestService>();

        // Assert
        service.Should().BeNull();
    }

    [Fact]
    public void GetRequiredService_BeforeCreateEncina_ShouldThrow()
    {
        // Act & Assert
        var act = () => _fixture.GetRequiredService<IEncina>();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CreateEncina*");
    }

    [Fact]
    public void ServiceProvider_BeforeCreateEncina_ShouldBeNull()
    {
        // Assert
        _fixture.ServiceProvider.Should().BeNull();
    }

    [Fact]
    public void ServiceProvider_AfterCreateEncina_ShouldNotBeNull()
    {
        // Arrange
        _fixture.CreateEncina(config =>
            config.RegisterServicesFromAssemblyContaining<TestRequest>());

        // Assert
        _fixture.ServiceProvider.Should().NotBeNull();
    }

    [Fact]
    public void CreateScope_ShouldReturnNewScope()
    {
        // Arrange
        _fixture.CreateEncina(config =>
            config.RegisterServicesFromAssemblyContaining<TestRequest>());

        // Act
        using var scope = _fixture.CreateScope();

        // Assert
        scope.Should().NotBeNull();
        scope.ServiceProvider.Should().NotBeNull();
    }

    [Fact]
    public void CreateScope_BeforeCreateEncina_ShouldThrow()
    {
        // Act & Assert
        var act = () => _fixture.CreateScope();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CreateEncina*");
    }

    [Fact]
    public void CreateEncinaFromAssemblyContaining_ShouldWork()
    {
        // Act
        var encina = _fixture.CreateEncinaFromAssemblyContaining<TestRequest>();

        // Assert
        encina.Should().NotBeNull();
    }

    public void Dispose() => _fixture.Dispose();

    #region Test Types

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
