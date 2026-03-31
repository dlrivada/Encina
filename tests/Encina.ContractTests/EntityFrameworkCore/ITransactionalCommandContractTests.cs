using System.Reflection;
using Encina.EntityFrameworkCore;
using Shouldly;

namespace Encina.ContractTests.EntityFrameworkCore;

/// <summary>
/// Contract tests verifying that <see cref="ITransactionalCommand"/> is a marker interface
/// and that <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> exists and handles it.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Transactions")]
public sealed class ITransactionalCommandContractTests
{
    private static readonly Type MarkerInterface = typeof(ITransactionalCommand);
    private static readonly Type PipelineBehaviorType = typeof(TransactionPipelineBehavior<,>);

    #region ITransactionalCommand - Marker Interface

    [Fact]
    public void ITransactionalCommand_ShouldBeAnInterface()
    {
        MarkerInterface.IsInterface.ShouldBeTrue(
            "ITransactionalCommand should be an interface");
    }

    [Fact]
    public void ITransactionalCommand_ShouldHaveNoMethods()
    {
        var methods = MarkerInterface.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        methods.Length.ShouldBe(0,
            "ITransactionalCommand should be a marker interface with no methods");
    }

    [Fact]
    public void ITransactionalCommand_ShouldHaveNoProperties()
    {
        var properties = MarkerInterface.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        properties.Length.ShouldBe(0,
            "ITransactionalCommand should be a marker interface with no properties");
    }

    [Fact]
    public void ITransactionalCommand_ShouldHaveNoEvents()
    {
        var events = MarkerInterface.GetEvents(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        events.Length.ShouldBe(0,
            "ITransactionalCommand should be a marker interface with no events");
    }

    [Fact]
    public void ITransactionalCommand_ShouldNotInheritOtherInterfaces()
    {
        var interfaces = MarkerInterface.GetInterfaces();

        interfaces.Length.ShouldBe(0,
            "ITransactionalCommand should not inherit from any other interface");
    }

    #endregion

    #region TransactionPipelineBehavior

    [Fact]
    public void TransactionPipelineBehavior_ShouldExist()
    {
        PipelineBehaviorType.ShouldNotBeNull(
            "TransactionPipelineBehavior<TRequest, TResponse> should exist");
    }

    [Fact]
    public void TransactionPipelineBehavior_ShouldBeSealed()
    {
        PipelineBehaviorType.IsSealed.ShouldBeTrue(
            "TransactionPipelineBehavior should be sealed");
    }

    [Fact]
    public void TransactionPipelineBehavior_ShouldBeGeneric()
    {
        PipelineBehaviorType.IsGenericType.ShouldBeTrue(
            "TransactionPipelineBehavior should be generic");
    }

    [Fact]
    public void TransactionPipelineBehavior_ShouldHaveTwoGenericParameters()
    {
        var genericArgs = PipelineBehaviorType.GetGenericArguments();

        genericArgs.Length.ShouldBe(2,
            "TransactionPipelineBehavior should have 2 generic type parameters (TRequest, TResponse)");
        genericArgs[0].Name.ShouldBe("TRequest");
        genericArgs[1].Name.ShouldBe("TResponse");
    }

    [Fact]
    public void TransactionPipelineBehavior_ShouldImplementIPipelineBehavior()
    {
        var interfaces = PipelineBehaviorType.GetInterfaces();

        interfaces.Select(i => i.IsGenericType ? i.GetGenericTypeDefinition().Name : i.Name)
            .ShouldContain("IPipelineBehavior`2",
                "TransactionPipelineBehavior should implement IPipelineBehavior<TRequest, TResponse>");
    }

    [Fact]
    public void TransactionPipelineBehavior_TRequest_ShouldHaveIRequestConstraint()
    {
        var tRequest = PipelineBehaviorType.GetGenericArguments()[0];
        var constraints = tRequest.GetGenericParameterConstraints();

        constraints.Length.ShouldBeGreaterThan(0,
            "TRequest should have at least one constraint (IRequest<TResponse>)");
    }

    #endregion

    #region TransactionPipelineBehavior Constructor

    [Fact]
    public void TransactionPipelineBehavior_ShouldHaveSingleConstructor()
    {
        var constructors = PipelineBehaviorType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        constructors.Length.ShouldBe(1,
            "TransactionPipelineBehavior should have exactly one public constructor");
    }

    [Fact]
    public void TransactionPipelineBehavior_Constructor_ShouldRequireDbContextAndLogger()
    {
        var constructor = PipelineBehaviorType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
        var parameters = constructor.GetParameters();

        parameters.Length.ShouldBe(2,
            "Constructor should have 2 parameters: DbContext, ILogger");

        parameters[0].ParameterType.FullName.ShouldBe(
            "Microsoft.EntityFrameworkCore.DbContext",
            "First parameter should be DbContext");

        parameters[1].ParameterType.Name.ShouldStartWith("ILogger");
    }

    #endregion
}
