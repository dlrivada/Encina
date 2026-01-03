using Encina.Testing.TUnit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Testing.TUnit.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaTUnitFixture"/>.
/// </summary>
public class EncinaTUnitFixtureTests
{
    [Test]
    public async Task InitializeAsync_ShouldCreateEncina()
    {
        // Arrange
        await using var fixture = new EncinaTUnitFixture();

        // Act
        await fixture.InitializeAsync();

        // Assert
        await Assert.That(fixture.Encina).IsNotNull();
        await Assert.That(fixture.ServiceProvider).IsNotNull();
    }

    [Test]
    public async Task Encina_AfterInitialize_ShouldReturnSameInstance()
    {
        // Arrange
        await using var fixture = new EncinaTUnitFixture();
        await fixture.InitializeAsync();

        // Act
        var encina1 = fixture.Encina;
        var encina2 = fixture.Encina;

        // Assert - verify both return the same instance
        await Assert.That(ReferenceEquals(encina1, encina2)).IsTrue();
    }

    [Test]
    public async Task Encina_BeforeInitialize_ShouldThrow()
    {
        // Arrange
        var fixture = new EncinaTUnitFixture();

        // Act & Assert
        await Assert.That(() => fixture.Encina)
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ServiceProvider_BeforeInitialize_ShouldThrow()
    {
        // Arrange
        var fixture = new EncinaTUnitFixture();

        // Act & Assert
        await Assert.That(() => fixture.ServiceProvider)
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task WithConfiguration_AfterInitialize_ShouldThrow()
    {
        // Arrange
        await using var fixture = new EncinaTUnitFixture();
        await fixture.InitializeAsync();

        // Act & Assert
        await Assert.That(() => fixture.WithConfiguration(_ => { }))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task WithServices_AfterInitialize_ShouldThrow()
    {
        // Arrange
        await using var fixture = new EncinaTUnitFixture();
        await fixture.InitializeAsync();

        // Act & Assert
        await Assert.That(() => fixture.WithServices(_ => { }))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task WithConfiguration_ShouldBeChainable()
    {
        // Arrange & Act
        await using var fixture = new EncinaTUnitFixture()
            .WithConfiguration(_ => { })
            .WithConfiguration(_ => { });

        await fixture.InitializeAsync();

        // Assert
        await Assert.That(fixture.Encina).IsNotNull();
    }

    [Test]
    public async Task WithServices_ShouldRegisterServices()
    {
        // Arrange
        await using var fixture = new EncinaTUnitFixture()
            .WithServices(services =>
            {
                services.AddSingleton<ITestService, TestService>();
            });

        await fixture.InitializeAsync();

        // Act
        var service = fixture.GetRequiredService<ITestService>();

        // Assert
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<TestService>();
    }

    [Test]
    public async Task GetService_WhenNotRegistered_ShouldReturnNull()
    {
        // Arrange
        await using var fixture = new EncinaTUnitFixture();
        await fixture.InitializeAsync();

        // Act
        var service = fixture.GetService<ITestService>();

        // Assert
        await Assert.That(service == null).IsTrue();
    }

    [Test]
    public async Task CreateScope_ShouldReturnServiceScope()
    {
        // Arrange
        await using var fixture = new EncinaTUnitFixture();
        await fixture.InitializeAsync();

        // Act
        using var scope = fixture.CreateScope();

        // Assert
        await Assert.That(scope).IsNotNull();
        await Assert.That(scope.ServiceProvider).IsNotNull();
    }

    [Test]
    public async Task DisposeAsync_ShouldAllowMultipleCalls()
    {
        // Arrange
        var fixture = new EncinaTUnitFixture();
        await fixture.InitializeAsync();

        // Act & Assert - should not throw
        await fixture.DisposeAsync();
        await fixture.DisposeAsync();
    }

    [Test]
    public async Task WithHandlersFromAssemblyContaining_ShouldRegisterHandlers()
    {
        // Arrange
        await using var fixture = new EncinaTUnitFixture()
            .WithHandlersFromAssemblyContaining<EncinaTUnitFixtureTests>();

        await fixture.InitializeAsync();

        // Act - Send a command to verify the handler was registered
        var result = await fixture.Encina.Send(new TestCommand("test-value"));

        // Assert
        await Assert.That(fixture.Encina).IsNotNull();
        var value = await result.ShouldBeSuccessAsync();
        await Assert.That(value).IsEqualTo("test-value");
    }

    [Test]
    public async Task WithConfiguration_NullAction_ShouldThrow()
    {
        // Arrange
        var fixture = new EncinaTUnitFixture();

        // Act & Assert
        await Assert.That(() => fixture.WithConfiguration(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task WithServices_NullAction_ShouldThrow()
    {
        // Arrange
        var fixture = new EncinaTUnitFixture();

        // Act & Assert
        await Assert.That(() => fixture.WithServices(null!))
            .Throws<ArgumentNullException>();
    }

    // Test service interface for DI testing
    private interface ITestService
    {
        string Name { get; }
    }

    // Test service implementation
    private sealed class TestService : ITestService
    {
        public string Name => "Test";
    }
}

// Test command for handler registration testing
internal sealed record TestCommand(string Value) : ICommand<string>;

// Test command handler
internal sealed class TestCommandHandler : ICommandHandler<TestCommand, string>
{
    public Task<Either<EncinaError, string>> Handle(TestCommand request, CancellationToken cancellationToken)
        => Task.FromResult(Right<EncinaError, string>(request.Value));
}
