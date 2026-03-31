using System.Reflection;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="EncinaConfiguration"/> to verify null and invalid parameter handling.
/// </summary>
public class EncinaConfigurationGuardTests
{
    private readonly EncinaConfiguration _sut = new();

    #region AddPipelineBehavior(Type)

    /// <summary>
    /// Verifies that AddPipelineBehavior throws ArgumentNullException when pipelineBehaviorType is null.
    /// </summary>
    [Fact]
    public void AddPipelineBehavior_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type pipelineBehaviorType = null!;

        // Act & Assert
        var act = () => _sut.AddPipelineBehavior(pipelineBehaviorType);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("pipelineBehaviorType");
    }

    /// <summary>
    /// Verifies that AddPipelineBehavior throws ArgumentException when type is an interface.
    /// </summary>
    [Fact]
    public void AddPipelineBehavior_InterfaceType_ThrowsArgumentException()
    {
        // Arrange
        var interfaceType = typeof(IPipelineBehavior<TestCommand, string>);

        // Act & Assert
        var act = () => _sut.AddPipelineBehavior(interfaceType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("pipelineBehaviorType");
    }

    /// <summary>
    /// Verifies that AddPipelineBehavior throws ArgumentException when type is abstract.
    /// </summary>
    [Fact]
    public void AddPipelineBehavior_AbstractType_ThrowsArgumentException()
    {
        // Arrange
        var abstractType = typeof(AbstractBehavior);

        // Act & Assert
        var act = () => _sut.AddPipelineBehavior(abstractType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("pipelineBehaviorType");
    }

    /// <summary>
    /// Verifies that AddPipelineBehavior throws ArgumentException when type does not implement IPipelineBehavior.
    /// </summary>
    [Fact]
    public void AddPipelineBehavior_TypeNotImplementingInterface_ThrowsArgumentException()
    {
        // Arrange
        var nonBehaviorType = typeof(NonBehaviorClass);

        // Act & Assert
        var act = () => _sut.AddPipelineBehavior(nonBehaviorType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("pipelineBehaviorType");
    }

    /// <summary>
    /// Verifies that AddPipelineBehavior succeeds with a valid concrete type and returns the configuration for chaining.
    /// </summary>
    [Fact]
    public void AddPipelineBehavior_ValidConcreteType_ReturnsSelf()
    {
        // Arrange
        var validType = typeof(ConcreteBehavior);

        // Act
        var result = _sut.AddPipelineBehavior(validType);

        // Assert
        result.ShouldBeSameAs(_sut);
        _sut.PipelineBehaviorTypes.ShouldContain(validType);
    }

    /// <summary>
    /// Verifies that AddPipelineBehavior does not add duplicates.
    /// </summary>
    [Fact]
    public void AddPipelineBehavior_DuplicateType_DoesNotAddTwice()
    {
        // Arrange
        var validType = typeof(ConcreteBehavior);
        _sut.AddPipelineBehavior(validType);

        // Act
        _sut.AddPipelineBehavior(validType);

        // Assert
        _sut.PipelineBehaviorTypes.Count(t => t == validType).ShouldBe(1);
    }

    #endregion

    #region AddRequestPreProcessor(Type)

    /// <summary>
    /// Verifies that AddRequestPreProcessor throws ArgumentNullException when processorType is null.
    /// </summary>
    [Fact]
    public void AddRequestPreProcessor_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type processorType = null!;

        // Act & Assert
        var act = () => _sut.AddRequestPreProcessor(processorType);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("processorType");
    }

    /// <summary>
    /// Verifies that AddRequestPreProcessor throws ArgumentException when type is an interface.
    /// </summary>
    [Fact]
    public void AddRequestPreProcessor_InterfaceType_ThrowsArgumentException()
    {
        // Arrange
        var interfaceType = typeof(IRequestPreProcessor<TestCommand>);

        // Act & Assert
        var act = () => _sut.AddRequestPreProcessor(interfaceType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorType");
    }

    /// <summary>
    /// Verifies that AddRequestPreProcessor throws ArgumentException when type is abstract.
    /// </summary>
    [Fact]
    public void AddRequestPreProcessor_AbstractType_ThrowsArgumentException()
    {
        // Arrange
        var abstractType = typeof(AbstractPreProcessor);

        // Act & Assert
        var act = () => _sut.AddRequestPreProcessor(abstractType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorType");
    }

    /// <summary>
    /// Verifies that AddRequestPreProcessor throws ArgumentException when type does not implement IRequestPreProcessor.
    /// </summary>
    [Fact]
    public void AddRequestPreProcessor_TypeNotImplementingInterface_ThrowsArgumentException()
    {
        // Arrange
        var nonProcessorType = typeof(NonBehaviorClass);

        // Act & Assert
        var act = () => _sut.AddRequestPreProcessor(nonProcessorType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorType");
    }

    /// <summary>
    /// Verifies that AddRequestPreProcessor succeeds with a valid concrete type.
    /// </summary>
    [Fact]
    public void AddRequestPreProcessor_ValidConcreteType_ReturnsSelf()
    {
        // Arrange
        var validType = typeof(ConcretePreProcessor);

        // Act
        var result = _sut.AddRequestPreProcessor(validType);

        // Assert
        result.ShouldBeSameAs(_sut);
        _sut.RequestPreProcessorTypes.ShouldContain(validType);
    }

    /// <summary>
    /// Verifies that AddRequestPreProcessor does not add duplicates.
    /// </summary>
    [Fact]
    public void AddRequestPreProcessor_DuplicateType_DoesNotAddTwice()
    {
        // Arrange
        var validType = typeof(ConcretePreProcessor);
        _sut.AddRequestPreProcessor(validType);

        // Act
        _sut.AddRequestPreProcessor(validType);

        // Assert
        _sut.RequestPreProcessorTypes.Count(t => t == validType).ShouldBe(1);
    }

    #endregion

    #region AddRequestPostProcessor(Type)

    /// <summary>
    /// Verifies that AddRequestPostProcessor throws ArgumentNullException when processorType is null.
    /// </summary>
    [Fact]
    public void AddRequestPostProcessor_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type processorType = null!;

        // Act & Assert
        var act = () => _sut.AddRequestPostProcessor(processorType);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("processorType");
    }

    /// <summary>
    /// Verifies that AddRequestPostProcessor throws ArgumentException when type is an interface.
    /// </summary>
    [Fact]
    public void AddRequestPostProcessor_InterfaceType_ThrowsArgumentException()
    {
        // Arrange
        var interfaceType = typeof(IRequestPostProcessor<TestCommand, string>);

        // Act & Assert
        var act = () => _sut.AddRequestPostProcessor(interfaceType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorType");
    }

    /// <summary>
    /// Verifies that AddRequestPostProcessor throws ArgumentException when type is abstract.
    /// </summary>
    [Fact]
    public void AddRequestPostProcessor_AbstractType_ThrowsArgumentException()
    {
        // Arrange
        var abstractType = typeof(AbstractPostProcessor);

        // Act & Assert
        var act = () => _sut.AddRequestPostProcessor(abstractType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorType");
    }

    /// <summary>
    /// Verifies that AddRequestPostProcessor throws ArgumentException when type does not implement IRequestPostProcessor.
    /// </summary>
    [Fact]
    public void AddRequestPostProcessor_TypeNotImplementingInterface_ThrowsArgumentException()
    {
        // Arrange
        var nonProcessorType = typeof(NonBehaviorClass);

        // Act & Assert
        var act = () => _sut.AddRequestPostProcessor(nonProcessorType);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorType");
    }

    /// <summary>
    /// Verifies that AddRequestPostProcessor succeeds with a valid concrete type.
    /// </summary>
    [Fact]
    public void AddRequestPostProcessor_ValidConcreteType_ReturnsSelf()
    {
        // Arrange
        var validType = typeof(ConcretePostProcessor);

        // Act
        var result = _sut.AddRequestPostProcessor(validType);

        // Assert
        result.ShouldBeSameAs(_sut);
        _sut.RequestPostProcessorTypes.ShouldContain(validType);
    }

    /// <summary>
    /// Verifies that AddRequestPostProcessor does not add duplicates.
    /// </summary>
    [Fact]
    public void AddRequestPostProcessor_DuplicateType_DoesNotAddTwice()
    {
        // Arrange
        var validType = typeof(ConcretePostProcessor);
        _sut.AddRequestPostProcessor(validType);

        // Act
        _sut.AddRequestPostProcessor(validType);

        // Assert
        _sut.RequestPostProcessorTypes.Count(t => t == validType).ShouldBe(1);
    }

    #endregion

    #region RegisterServicesFromAssembly

    /// <summary>
    /// Verifies that RegisterServicesFromAssembly throws ArgumentNullException when assembly is null.
    /// </summary>
    [Fact]
    public void RegisterServicesFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        Assembly assembly = null!;

        // Act & Assert
        var act = () => _sut.RegisterServicesFromAssembly(assembly);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("assembly");
    }

    /// <summary>
    /// Verifies that RegisterServicesFromAssembly succeeds with a valid assembly and returns self.
    /// </summary>
    [Fact]
    public void RegisterServicesFromAssembly_ValidAssembly_ReturnsSelfAndAdds()
    {
        // Arrange
        var assembly = typeof(EncinaConfiguration).Assembly;

        // Act
        var result = _sut.RegisterServicesFromAssembly(assembly);

        // Assert
        result.ShouldBeSameAs(_sut);
        _sut.Assemblies.ShouldContain(assembly);
    }

    #endregion

    #region RegisterServicesFromAssemblies

    /// <summary>
    /// Verifies that RegisterServicesFromAssemblies handles null array gracefully (returns self, no throw).
    /// </summary>
    [Fact]
    public void RegisterServicesFromAssemblies_NullArray_ReturnsSelfWithoutThrow()
    {
        // Arrange
        Assembly[] assemblies = null!;

        // Act
        var result = _sut.RegisterServicesFromAssemblies(assemblies);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    /// <summary>
    /// Verifies that RegisterServicesFromAssemblies skips null entries in the array.
    /// </summary>
    [Fact]
    public void RegisterServicesFromAssemblies_ArrayWithNullEntries_SkipsNulls()
    {
        // Arrange
        var validAssembly = typeof(EncinaConfiguration).Assembly;
        Assembly[] assemblies = [validAssembly, null!];

        // Act
        var result = _sut.RegisterServicesFromAssemblies(assemblies);

        // Assert
        result.ShouldBeSameAs(_sut);
        _sut.Assemblies.Count.ShouldBe(1);
        _sut.Assemblies.ShouldContain(validAssembly);
    }

    /// <summary>
    /// Verifies that RegisterServicesFromAssemblies adds all valid assemblies.
    /// </summary>
    [Fact]
    public void RegisterServicesFromAssemblies_MultipleValidAssemblies_AddsAll()
    {
        // Arrange
        var assembly1 = typeof(EncinaConfiguration).Assembly;
        var assembly2 = typeof(EncinaConfigurationGuardTests).Assembly;

        // Act
        _sut.RegisterServicesFromAssemblies(assembly1, assembly2);

        // Assert
        _sut.Assemblies.Count.ShouldBe(2);
    }

    #endregion

    #region UseParallelNotificationDispatch

    /// <summary>
    /// Verifies that UseParallelNotificationDispatch sets the strategy correctly and returns self.
    /// </summary>
    [Fact]
    public void UseParallelNotificationDispatch_SetsStrategyAndReturnsSelf()
    {
        // Act
        var result = _sut.UseParallelNotificationDispatch(NotificationDispatchStrategy.ParallelWhenAll, maxDegreeOfParallelism: 4);

        // Assert
        result.ShouldBeSameAs(_sut);
        _sut.NotificationDispatch.Strategy.ShouldBe(NotificationDispatchStrategy.ParallelWhenAll);
        _sut.NotificationDispatch.MaxDegreeOfParallelism.ShouldBe(4);
    }

    /// <summary>
    /// Verifies that UseParallelNotificationDispatch defaults correctly.
    /// </summary>
    [Fact]
    public void UseParallelNotificationDispatch_DefaultParameters_UsesParallelAndNegativeOne()
    {
        // Act
        _sut.UseParallelNotificationDispatch();

        // Assert
        _sut.NotificationDispatch.Strategy.ShouldBe(NotificationDispatchStrategy.Parallel);
        _sut.NotificationDispatch.MaxDegreeOfParallelism.ShouldBe(-1);
    }

    #endregion

    #region WithHandlerLifetime

    /// <summary>
    /// Verifies that WithHandlerLifetime sets the lifetime and returns self.
    /// </summary>
    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void WithHandlerLifetime_ValidLifetime_SetsAndReturnsSelf(ServiceLifetime lifetime)
    {
        // Act
        var result = _sut.WithHandlerLifetime(lifetime);

        // Assert
        result.ShouldBeSameAs(_sut);
        _sut.HandlerLifetime.ShouldBe(lifetime);
    }

    #endregion

    #region RegisterServicesFromAssemblyContaining

    /// <summary>
    /// Verifies that RegisterServicesFromAssemblyContaining adds the correct assembly.
    /// </summary>
    [Fact]
    public void RegisterServicesFromAssemblyContaining_AddsCorrectAssembly()
    {
        // Act
        _sut.RegisterServicesFromAssemblyContaining<EncinaConfiguration>();

        // Assert
        _sut.Assemblies.ShouldContain(typeof(EncinaConfiguration).Assembly);
    }

    #endregion

    #region RegisterConfiguredPipelineBehaviors (internal)

    /// <summary>
    /// Verifies that RegisterConfiguredPipelineBehaviors throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void RegisterConfiguredPipelineBehaviors_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => _sut.RegisterConfiguredPipelineBehaviors(services);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that RegisterConfiguredRequestPreProcessors throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void RegisterConfiguredRequestPreProcessors_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => _sut.RegisterConfiguredRequestPreProcessors(services);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that RegisterConfiguredRequestPostProcessors throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void RegisterConfiguredRequestPostProcessors_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => _sut.RegisterConfiguredRequestPostProcessors(services);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    #endregion

    #region Test Stubs

    private sealed class TestCommand : ICommand<string> { }

    private abstract class AbstractBehavior : IPipelineBehavior<TestCommand, string>
    {
        public abstract ValueTask<Either<EncinaError, string>> Handle(TestCommand request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken);
    }

    private sealed class ConcreteBehavior : IPipelineBehavior<TestCommand, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(TestCommand request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class NonBehaviorClass { }

    private abstract class AbstractPreProcessor : IRequestPreProcessor<TestCommand>
    {
        public abstract Task Process(TestCommand request, IRequestContext context, CancellationToken cancellationToken);
    }

    private sealed class ConcretePreProcessor : IRequestPreProcessor<TestCommand>
    {
        public Task Process(TestCommand request, IRequestContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private abstract class AbstractPostProcessor : IRequestPostProcessor<TestCommand, string>
    {
        public abstract Task Process(TestCommand request, IRequestContext context, Either<EncinaError, string> response, CancellationToken cancellationToken);
    }

    private sealed class ConcretePostProcessor : IRequestPostProcessor<TestCommand, string>
    {
        public Task Process(TestCommand request, IRequestContext context, Either<EncinaError, string> response, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    #endregion
}
