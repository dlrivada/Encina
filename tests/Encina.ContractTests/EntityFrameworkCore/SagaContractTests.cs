using System.Reflection;
using Encina.EntityFrameworkCore.Sagas;
using LanguageExt;
using Shouldly;

namespace Encina.ContractTests.EntityFrameworkCore;

/// <summary>
/// Contract tests verifying that <see cref="Saga{TSagaData}"/> abstract class
/// defines the expected members with correct signatures, visibility, and return types.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Sagas")]
public sealed class SagaContractTests
{
    private static readonly Type SagaOpenType = typeof(Saga<>);

    #region Class Shape

    [Fact]
    public void Saga_ShouldBeAbstract()
    {
        SagaOpenType.IsAbstract.ShouldBeTrue(
            "Saga<TSagaData> should be an abstract class");
    }

    [Fact]
    public void Saga_ShouldBeGeneric()
    {
        SagaOpenType.IsGenericType.ShouldBeTrue(
            "Saga<TSagaData> should be a generic type");
    }

    [Fact]
    public void Saga_ShouldHaveOneGenericParameter()
    {
        var genericArgs = SagaOpenType.GetGenericArguments();

        genericArgs.Length.ShouldBe(1,
            "Saga<TSagaData> should have exactly one generic type parameter");
        genericArgs[0].Name.ShouldBe("TSagaData");
    }

    [Fact]
    public void Saga_TSagaData_ShouldHaveClassAndNewConstraints()
    {
        var genericArg = SagaOpenType.GetGenericArguments()[0];
        var constraints = genericArg.GenericParameterAttributes;

        // class constraint
        (constraints & GenericParameterAttributes.ReferenceTypeConstraint).ShouldNotBe(
            (GenericParameterAttributes)0,
            "TSagaData should have a 'class' constraint");

        // new() constraint
        (constraints & GenericParameterAttributes.DefaultConstructorConstraint).ShouldNotBe(
            (GenericParameterAttributes)0,
            "TSagaData should have a 'new()' constraint");
    }

    #endregion

    #region ConfigureSteps

    [Fact]
    public void ConfigureSteps_ShouldExist()
    {
        var method = SagaOpenType.GetMethod("ConfigureSteps",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull(
            "Saga<TSagaData> should have a ConfigureSteps method");
    }

    [Fact]
    public void ConfigureSteps_ShouldBeAbstract()
    {
        var method = SagaOpenType.GetMethod("ConfigureSteps",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method!.IsAbstract.ShouldBeTrue(
            "ConfigureSteps should be abstract");
    }

    [Fact]
    public void ConfigureSteps_ShouldBeProtected()
    {
        var method = SagaOpenType.GetMethod("ConfigureSteps",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method!.IsFamily.ShouldBeTrue(
            "ConfigureSteps should be protected (Family)");
    }

    [Fact]
    public void ConfigureSteps_ShouldReturnVoid()
    {
        var method = SagaOpenType.GetMethod("ConfigureSteps",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(void),
            "ConfigureSteps should return void");
    }

    [Fact]
    public void ConfigureSteps_ShouldHaveNoParameters()
    {
        var method = SagaOpenType.GetMethod("ConfigureSteps",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method!.GetParameters().Length.ShouldBe(0,
            "ConfigureSteps should have no parameters");
    }

    #endregion

    #region AddStep

    [Fact]
    public void AddStep_ShouldExist()
    {
        var method = SagaOpenType.GetMethod("AddStep",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull(
            "Saga<TSagaData> should have an AddStep method");
    }

    [Fact]
    public void AddStep_ShouldBeProtected()
    {
        var method = SagaOpenType.GetMethod("AddStep",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method!.IsFamily.ShouldBeTrue(
            "AddStep should be protected (Family)");
    }

    [Fact]
    public void AddStep_ShouldHaveTwoParameters()
    {
        var method = SagaOpenType.GetMethod("AddStep",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull();
        var parameters = method!.GetParameters();

        parameters.Length.ShouldBe(2,
            "AddStep should have 2 parameters: execute and compensate");
    }

    [Fact]
    public void AddStep_ExecuteParameter_ShouldBeFunc()
    {
        var method = SagaOpenType.GetMethod("AddStep",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull();
        var executeParam = method!.GetParameters()[0];

        executeParam.Name.ShouldBe("execute");
        executeParam.ParameterType.Name.ShouldStartWith("Func");
    }

    [Fact]
    public void AddStep_CompensateParameter_ShouldBeNullableFunc()
    {
        var method = SagaOpenType.GetMethod("AddStep",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method.ShouldNotBeNull();
        var compensateParam = method!.GetParameters()[1];

        compensateParam.Name.ShouldBe("compensate");
        compensateParam.HasDefaultValue.ShouldBeTrue(
            "compensate parameter should have a default value (null)");
    }

    #endregion

    #region ExecuteAsync

    [Fact]
    public void ExecuteAsync_ShouldExist()
    {
        var method = SagaOpenType.GetMethod("ExecuteAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull(
            "Saga<TSagaData> should have a public ExecuteAsync method");
    }

    [Fact]
    public void ExecuteAsync_ShouldBePublic()
    {
        var method = SagaOpenType.GetMethod("ExecuteAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method!.IsPublic.ShouldBeTrue(
            "ExecuteAsync should be public");
    }

    [Fact]
    public void ExecuteAsync_ShouldHaveFourParameters()
    {
        var method = SagaOpenType.GetMethod("ExecuteAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        var parameters = method!.GetParameters();

        parameters.Length.ShouldBe(4,
            "ExecuteAsync should have 4 parameters: TSagaData, int, IRequestContext, CancellationToken");
    }

    [Fact]
    public void ExecuteAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = SagaOpenType.GetMethod("ExecuteAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        var parameters = method!.GetParameters();

        parameters[0].Name.ShouldBe("data");
        parameters[1].ParameterType.ShouldBe(typeof(int),
            "Second parameter (currentStep) should be int");
        parameters[1].Name.ShouldBe("currentStep");
        parameters[2].ParameterType.ShouldBe(typeof(IRequestContext),
            "Third parameter should be IRequestContext");
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken),
            "Fourth parameter should be CancellationToken");
    }

    [Fact]
    public void ExecuteAsync_ShouldReturnValueTaskOfEither()
    {
        var method = SagaOpenType.GetMethod("ExecuteAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();

        // Return type should be ValueTask<Either<EncinaError, TSagaData>>
        var returnType = method!.ReturnType;
        returnType.IsGenericType.ShouldBeTrue(
            "Return type should be generic (ValueTask<...>)");
        returnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>),
            "Return type should be ValueTask<>");
    }

    #endregion

    #region CompensateAsync

    [Fact]
    public void CompensateAsync_ShouldExist()
    {
        var method = SagaOpenType.GetMethod("CompensateAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull(
            "Saga<TSagaData> should have a public CompensateAsync method");
    }

    [Fact]
    public void CompensateAsync_ShouldBePublic()
    {
        var method = SagaOpenType.GetMethod("CompensateAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method!.IsPublic.ShouldBeTrue(
            "CompensateAsync should be public");
    }

    [Fact]
    public void CompensateAsync_ShouldReturnTask()
    {
        var method = SagaOpenType.GetMethod("CompensateAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task),
            "CompensateAsync should return Task");
    }

    [Fact]
    public void CompensateAsync_ShouldHaveFourParameters()
    {
        var method = SagaOpenType.GetMethod("CompensateAsync",
            BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        var parameters = method!.GetParameters();

        parameters.Length.ShouldBe(4,
            "CompensateAsync should have 4 parameters: TSagaData, int, IRequestContext, CancellationToken");

        parameters[0].Name.ShouldBe("data");
        parameters[1].ParameterType.ShouldBe(typeof(int),
            "Second parameter (fromStep) should be int");
        parameters[1].Name.ShouldBe("fromStep");
        parameters[2].ParameterType.ShouldBe(typeof(IRequestContext),
            "Third parameter should be IRequestContext");
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken),
            "Fourth parameter should be CancellationToken");
    }

    #endregion

    #region StepCount Property

    [Fact]
    public void StepCount_ShouldExist()
    {
        var property = SagaOpenType.GetProperty("StepCount",
            BindingFlags.Public | BindingFlags.Instance);

        property.ShouldNotBeNull(
            "Saga<TSagaData> should have a public StepCount property");
    }

    [Fact]
    public void StepCount_ShouldReturnInt()
    {
        var property = SagaOpenType.GetProperty("StepCount",
            BindingFlags.Public | BindingFlags.Instance);

        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(int),
            "StepCount should be of type int");
    }

    [Fact]
    public void StepCount_ShouldHaveGetter()
    {
        var property = SagaOpenType.GetProperty("StepCount",
            BindingFlags.Public | BindingFlags.Instance);

        property.ShouldNotBeNull();
        property!.GetMethod.ShouldNotBeNull(
            "StepCount should have a getter");
        property.GetMethod!.IsPublic.ShouldBeTrue(
            "StepCount getter should be public");
    }

    [Fact]
    public void StepCount_ShouldNotHaveSetter()
    {
        var property = SagaOpenType.GetProperty("StepCount",
            BindingFlags.Public | BindingFlags.Instance);

        property.ShouldNotBeNull();
        property!.SetMethod.ShouldBeNull(
            "StepCount should not have a public setter");
    }

    #endregion

    #region SagaStep Internal Class

    [Fact]
    public void SagaStep_ShouldBeInternal()
    {
        var sagaStepType = SagaOpenType.Assembly.GetType("Encina.EntityFrameworkCore.Sagas.SagaStep`1");

        sagaStepType.ShouldNotBeNull(
            "SagaStep<TSagaData> should exist in the assembly");
        sagaStepType!.IsPublic.ShouldBeFalse(
            "SagaStep<TSagaData> should not be public");
    }

    [Fact]
    public void SagaStep_ShouldBeSealed()
    {
        var sagaStepType = SagaOpenType.Assembly.GetType("Encina.EntityFrameworkCore.Sagas.SagaStep`1");

        sagaStepType.ShouldNotBeNull();
        sagaStepType!.IsSealed.ShouldBeTrue(
            "SagaStep<TSagaData> should be sealed");
    }

    #endregion
}
