using System.Reflection;
using Encina.Sharding.Resharding;
using LanguageExt;
using Shouldly;

namespace Encina.ContractTests.Sharding.Resharding;

/// <summary>
/// Contract tests verifying the public API surface of the resharding interfaces
/// using reflection. Ensures method counts, parameter types, return types,
/// and CancellationToken default values are correct.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ReshardingContractTests
{
    private static readonly BindingFlags InterfaceMethods =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    #region IReshardingOrchestrator Method Count

    [Fact]
    public void Contract_IReshardingOrchestrator_HasExactly4Methods()
    {
        var methods = typeof(IReshardingOrchestrator).GetMethods(InterfaceMethods);

        methods.Length.ShouldBe(4,
            "IReshardingOrchestrator should declare exactly 4 methods: PlanAsync, ExecuteAsync, RollbackAsync, GetProgressAsync");
    }

    #endregion

    #region IReshardingOrchestrator Method Signatures

    [Fact]
    public void Contract_IReshardingOrchestrator_PlanAsync_HasCorrectSignature()
    {
        var method = typeof(IReshardingOrchestrator).GetMethod(
            nameof(IReshardingOrchestrator.PlanAsync), InterfaceMethods);

        method.ShouldNotBeNull("PlanAsync should exist on IReshardingOrchestrator");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "PlanAsync should have 2 parameters");

        parameters[0].ParameterType.ShouldBe(typeof(ReshardingRequest),
            "PlanAsync first parameter should be ReshardingRequest");
        parameters[0].Name.ShouldBe("request");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
            "PlanAsync second parameter should be CancellationToken");

        method.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, ReshardingPlan>>),
            "PlanAsync should return Task<Either<EncinaError, ReshardingPlan>>");
    }

    [Fact]
    public void Contract_IReshardingOrchestrator_ExecuteAsync_HasCorrectSignature()
    {
        var method = typeof(IReshardingOrchestrator).GetMethod(
            nameof(IReshardingOrchestrator.ExecuteAsync), InterfaceMethods);

        method.ShouldNotBeNull("ExecuteAsync should exist on IReshardingOrchestrator");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3, "ExecuteAsync should have 3 parameters");

        parameters[0].ParameterType.ShouldBe(typeof(ReshardingPlan),
            "ExecuteAsync first parameter should be ReshardingPlan");
        parameters[0].Name.ShouldBe("plan");

        parameters[1].ParameterType.ShouldBe(typeof(ReshardingOptions),
            "ExecuteAsync second parameter should be ReshardingOptions");
        parameters[1].Name.ShouldBe("options");

        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken),
            "ExecuteAsync third parameter should be CancellationToken");

        method.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, ReshardingResult>>),
            "ExecuteAsync should return Task<Either<EncinaError, ReshardingResult>>");
    }

    [Fact]
    public void Contract_IReshardingOrchestrator_RollbackAsync_HasCorrectSignature()
    {
        var method = typeof(IReshardingOrchestrator).GetMethod(
            nameof(IReshardingOrchestrator.RollbackAsync), InterfaceMethods);

        method.ShouldNotBeNull("RollbackAsync should exist on IReshardingOrchestrator");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "RollbackAsync should have 2 parameters");

        parameters[0].ParameterType.ShouldBe(typeof(ReshardingResult),
            "RollbackAsync first parameter should be ReshardingResult");
        parameters[0].Name.ShouldBe("result");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
            "RollbackAsync second parameter should be CancellationToken");

        method.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, Unit>>),
            "RollbackAsync should return Task<Either<EncinaError, Unit>>");
    }

    [Fact]
    public void Contract_IReshardingOrchestrator_GetProgressAsync_HasCorrectSignature()
    {
        var method = typeof(IReshardingOrchestrator).GetMethod(
            nameof(IReshardingOrchestrator.GetProgressAsync), InterfaceMethods);

        method.ShouldNotBeNull("GetProgressAsync should exist on IReshardingOrchestrator");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "GetProgressAsync should have 2 parameters");

        parameters[0].ParameterType.ShouldBe(typeof(Guid),
            "GetProgressAsync first parameter should be Guid");
        parameters[0].Name.ShouldBe("reshardingId");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
            "GetProgressAsync second parameter should be CancellationToken");

        method.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, ReshardingProgress>>),
            "GetProgressAsync should return Task<Either<EncinaError, ReshardingProgress>>");
    }

    [Fact]
    public void Contract_IReshardingOrchestrator_AllMethods_HaveCancellationTokenWithDefaultValue()
    {
        var methods = typeof(IReshardingOrchestrator).GetMethods(InterfaceMethods);

        foreach (var method in methods)
        {
            var lastParam = method.GetParameters().Last();

            lastParam.ParameterType.ShouldBe(typeof(CancellationToken),
                $"{method.Name}: last parameter should be CancellationToken");
            lastParam.HasDefaultValue.ShouldBeTrue(
                $"{method.Name}: CancellationToken parameter should have a default value");
        }
    }

    #endregion

    #region IReshardingStateStore Method Count

    [Fact]
    public void Contract_IReshardingStateStore_HasExactly4Methods()
    {
        var methods = typeof(IReshardingStateStore).GetMethods(InterfaceMethods);

        methods.Length.ShouldBe(4,
            "IReshardingStateStore should declare exactly 4 methods: SaveStateAsync, GetStateAsync, GetActiveReshardingsAsync, DeleteStateAsync");
    }

    #endregion

    #region IReshardingStateStore Method Signatures

    [Fact]
    public void Contract_IReshardingStateStore_SaveStateAsync_HasCorrectSignature()
    {
        var method = typeof(IReshardingStateStore).GetMethod(
            nameof(IReshardingStateStore.SaveStateAsync), InterfaceMethods);

        method.ShouldNotBeNull("SaveStateAsync should exist on IReshardingStateStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "SaveStateAsync should have 2 parameters");

        parameters[0].ParameterType.ShouldBe(typeof(ReshardingState),
            "SaveStateAsync first parameter should be ReshardingState");
        parameters[0].Name.ShouldBe("state");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
            "SaveStateAsync second parameter should be CancellationToken");

        method.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, Unit>>),
            "SaveStateAsync should return Task<Either<EncinaError, Unit>>");
    }

    [Fact]
    public void Contract_IReshardingStateStore_GetStateAsync_HasCorrectSignature()
    {
        var method = typeof(IReshardingStateStore).GetMethod(
            nameof(IReshardingStateStore.GetStateAsync), InterfaceMethods);

        method.ShouldNotBeNull("GetStateAsync should exist on IReshardingStateStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "GetStateAsync should have 2 parameters");

        parameters[0].ParameterType.ShouldBe(typeof(Guid),
            "GetStateAsync first parameter should be Guid");
        parameters[0].Name.ShouldBe("reshardingId");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
            "GetStateAsync second parameter should be CancellationToken");

        method.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, ReshardingState>>),
            "GetStateAsync should return Task<Either<EncinaError, ReshardingState>>");
    }

    [Fact]
    public void Contract_IReshardingStateStore_GetActiveReshardingsAsync_HasCorrectSignature()
    {
        var method = typeof(IReshardingStateStore).GetMethod(
            nameof(IReshardingStateStore.GetActiveReshardingsAsync), InterfaceMethods);

        method.ShouldNotBeNull("GetActiveReshardingsAsync should exist on IReshardingStateStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1, "GetActiveReshardingsAsync should have 1 parameter");

        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken),
            "GetActiveReshardingsAsync parameter should be CancellationToken");

        method.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, IReadOnlyList<ReshardingState>>>),
            "GetActiveReshardingsAsync should return Task<Either<EncinaError, IReadOnlyList<ReshardingState>>>");
    }

    [Fact]
    public void Contract_IReshardingStateStore_DeleteStateAsync_HasCorrectSignature()
    {
        var method = typeof(IReshardingStateStore).GetMethod(
            nameof(IReshardingStateStore.DeleteStateAsync), InterfaceMethods);

        method.ShouldNotBeNull("DeleteStateAsync should exist on IReshardingStateStore");

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "DeleteStateAsync should have 2 parameters");

        parameters[0].ParameterType.ShouldBe(typeof(Guid),
            "DeleteStateAsync first parameter should be Guid");
        parameters[0].Name.ShouldBe("reshardingId");

        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
            "DeleteStateAsync second parameter should be CancellationToken");

        method.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, Unit>>),
            "DeleteStateAsync should return Task<Either<EncinaError, Unit>>");
    }

    #endregion

    #region IReshardingServices Method Count

    [Fact]
    public void Contract_IReshardingServices_HasExactly7Methods()
    {
        var methods = typeof(IReshardingServices).GetMethods(InterfaceMethods);

        methods.Length.ShouldBe(7,
            "IReshardingServices should declare exactly 7 methods");
    }

    #endregion

    #region IReshardingServices CancellationToken Contract

    [Fact]
    public void Contract_IReshardingServices_AllMethods_HaveCancellationTokenAsLastParameter()
    {
        var methods = typeof(IReshardingServices).GetMethods(InterfaceMethods);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            parameters.Length.ShouldBeGreaterThan(0,
                $"{method.Name}: should have at least one parameter");

            var lastParam = parameters.Last();
            lastParam.ParameterType.ShouldBe(typeof(CancellationToken),
                $"{method.Name}: last parameter should be CancellationToken");
        }
    }

    [Fact]
    public void Contract_IReshardingServices_AllCancellationTokens_HaveDefaultValue()
    {
        var methods = typeof(IReshardingServices).GetMethods(InterfaceMethods);

        foreach (var method in methods)
        {
            var lastParam = method.GetParameters().Last();

            lastParam.ParameterType.ShouldBe(typeof(CancellationToken),
                $"{method.Name}: last parameter should be CancellationToken");
            lastParam.HasDefaultValue.ShouldBeTrue(
                $"{method.Name}: CancellationToken parameter should have a default value");
        }
    }

    #endregion

    #region All Methods Return Task<Either<...>>

    [Fact]
    public void Contract_IReshardingOrchestrator_AllMethods_ReturnTaskOfEither()
    {
        VerifyAllMethodsReturnTaskOfEither(typeof(IReshardingOrchestrator));
    }

    [Fact]
    public void Contract_IReshardingStateStore_AllMethods_ReturnTaskOfEither()
    {
        VerifyAllMethodsReturnTaskOfEither(typeof(IReshardingStateStore));
    }

    [Fact]
    public void Contract_IReshardingServices_AllMethods_ReturnTaskOfEither()
    {
        VerifyAllMethodsReturnTaskOfEither(typeof(IReshardingServices));
    }

    #endregion

    #region Helpers

    private static void VerifyAllMethodsReturnTaskOfEither(Type interfaceType)
    {
        var methods = interfaceType.GetMethods(InterfaceMethods);

        foreach (var method in methods)
        {
            var returnType = method.ReturnType;

            returnType.IsGenericType.ShouldBeTrue(
                $"{interfaceType.Name}.{method.Name}: return type should be a generic type");
            returnType.GetGenericTypeDefinition().ShouldBe(typeof(Task<>),
                $"{interfaceType.Name}.{method.Name}: return type should be Task<T>");

            var innerType = returnType.GetGenericArguments()[0];
            innerType.IsGenericType.ShouldBeTrue(
                $"{interfaceType.Name}.{method.Name}: inner type should be generic (Either<,>)");
            innerType.GetGenericTypeDefinition().ShouldBe(typeof(Either<,>),
                $"{interfaceType.Name}.{method.Name}: inner type should be Either<EncinaError, T>");

            var eitherArgs = innerType.GetGenericArguments();
            eitherArgs[0].ShouldBe(typeof(EncinaError),
                $"{interfaceType.Name}.{method.Name}: Either left type should be EncinaError");
        }
    }

    #endregion
}
