using Encina.Modules;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Modules;

public sealed class ModuleBehaviorAdapterTests
{
    [Fact]
    public async Task Handle_WhenHandlerBelongsToTargetModule_ExecutesBehavior()
    {
        // Arrange
        var behavior = new TestModuleBehavior();
        var module = new TestModule();
        var registry = CreateRegistryForModule(module);

        var adapter = new ModuleBehaviorAdapter<TestModule, TestRequest, string>(
            behavior, module, registry);

        var request = new TestRequest();
        var context = RequestContext.Create();
        var nextStepCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("next-result"));
        };

        // Act
        var result = await adapter.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        behavior.WasCalled.ShouldBeTrue();
        nextStepCalled.ShouldBeTrue();
        var errorMsg = result.Match(Left: e => e.Message, Right: _ => "");
        result.IsRight.ShouldBeTrue($"Expected Right but got Left: {errorMsg}");
        result.IfRight(r => r.ShouldBe("behavior-executed:next-result"));
    }

    [Fact]
    public async Task Handle_WhenHandlerDoesNotBelongToTargetModule_SkipsBehavior()
    {
        // Arrange
        var behavior = new TestModuleBehavior();
        var module = new TestModule();
        var registry = CreateEmptyRegistry(); // Handler not in this module

        var adapter = new ModuleBehaviorAdapter<TestModule, TestRequest, string>(
            behavior, module, registry);

        var request = new TestRequest();
        var context = RequestContext.Create();
        var nextStepCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("next-result"));
        };

        // Act
        var result = await adapter.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        behavior.WasCalled.ShouldBeFalse();
        nextStepCalled.ShouldBeTrue();
        var errorMsg2 = result.Match(Left: e => e.Message, Right: _ => "");
        result.IsRight.ShouldBeTrue($"Expected Right but got Left: {errorMsg2}");
        result.IfRight(r => r.ShouldBe("next-result"));
    }

    [Fact]
    public void Constructor_WithNullBehavior_ThrowsArgumentNullException()
    {
        // Arrange
        var module = new TestModule();
        var registry = CreateEmptyRegistry();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ModuleBehaviorAdapter<TestModule, TestRequest, string>(null!, module, registry));
    }

    [Fact]
    public void Constructor_WithNullModule_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = new TestModuleBehavior();
        var registry = CreateEmptyRegistry();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ModuleBehaviorAdapter<TestModule, TestRequest, string>(behavior, null!, registry));
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = new TestModuleBehavior();
        var module = new TestModule();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ModuleBehaviorAdapter<TestModule, TestRequest, string>(behavior, module, null!));
    }

    [Fact]
    public async Task Handle_WhenBehaviorReturnsError_PropagatesError()
    {
        // Arrange
        var behavior = new ErrorReturningBehavior();
        var module = new TestModule();
        var registry = CreateRegistryForModule(module);

        var adapter = new ModuleBehaviorAdapter<TestModule, TestRequest, string>(
            behavior, module, registry);

        var request = new TestRequest();
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("next-result"));

        // Act
        var result = await adapter.Handle(request, context, nextStep, CancellationToken.None);

        // Assert with better diagnostics
        var errorMessage = result.Match(Left: e => $"Left({e.Message})", Right: r => $"Right({r})");
        result.IsLeft.ShouldBeTrue($"Result should be Left but was: {errorMessage}");
        result.IfLeft(e => e.Message.ShouldBe("behavior-error"));
    }

    #region Helpers

    private static IModuleHandlerRegistry CreateRegistryForModule(IModule module)
    {
        var registry = Substitute.For<IModuleHandlerRegistry>();
        // Configure mock to return true for any call to BelongsToModule
        registry.BelongsToModule(Arg.Any<Type>(), Arg.Any<string>())
            .Returns(true);
        return registry;
    }

    private static IModuleHandlerRegistry CreateEmptyRegistry()
    {
        var registry = Substitute.For<IModuleHandlerRegistry>();
        registry.BelongsToModule(Arg.Any<Type>(), Arg.Any<string>())
            .Returns(false);
        return registry;
    }

    #endregion

    #region Test Fixtures

    private sealed class TestModule : IModule
    {
        public string Name => "TestModule";
        public void ConfigureServices(IServiceCollection services) { }
    }

    private sealed record TestRequest : IRequest<string>;

    private sealed class TestModuleBehavior : IModulePipelineBehavior<TestModule, TestRequest, string>
    {
        public bool WasCalled { get; private set; }

        public async ValueTask<Either<EncinaError, string>> Handle(
            TestRequest request,
            IRequestContext context,
            RequestHandlerCallback<string> nextStep,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            var result = await nextStep();
            return result.Map(r => $"behavior-executed:{r}");
        }
    }

    private sealed class ErrorReturningBehavior : IModulePipelineBehavior<TestModule, TestRequest, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            TestRequest request,
            IRequestContext context,
            RequestHandlerCallback<string> nextStep,
            CancellationToken cancellationToken)
        {
            return new ValueTask<Either<EncinaError, string>>(
                Left<EncinaError, string>(EncinaError.New("behavior-error")));
        }
    }

    #endregion
}
