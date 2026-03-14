#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;

using LanguageExt;

namespace Encina.ContractTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Contract tests verifying that <see cref="ITIAService"/> follows expected API design contracts.
/// </summary>
[Trait("Category", "Contract")]
public class ITIAServiceContractTests
{
    private static readonly Type InterfaceType = typeof(ITIAService);

    private static readonly string[] RequiredMethods =
    [
        nameof(ITIAService.CreateTIAAsync),
        nameof(ITIAService.AssessRiskAsync),
        nameof(ITIAService.RequireSupplementaryMeasureAsync),
        nameof(ITIAService.SubmitForDPOReviewAsync),
        nameof(ITIAService.CompleteDPOReviewAsync),
        nameof(ITIAService.GetTIAAsync),
        nameof(ITIAService.GetTIAByRouteAsync)
    ];

    [Fact]
    public void ITIAService_ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void ITIAService_ShouldHaveExactlySevenMethods()
    {
        var methods = InterfaceType.GetMethods();
        methods.Length.ShouldBe(7);
    }

    [Fact]
    public void ITIAService_ShouldHaveAllRequiredMethods()
    {
        foreach (var methodName in RequiredMethods)
        {
            var method = InterfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            method.ShouldNotBeNull($"ITIAService must define {methodName}");
        }
    }

    [Fact]
    public void CreateTIAAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.CreateTIAAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Guid>>));
    }

    [Fact]
    public void CreateTIAAsync_ShouldHaveCorrectParameters()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.CreateTIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(7); // sourceCountryCode, destinationCountryCode, dataCategory, createdBy, tenantId, moduleId, CancellationToken
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[3].ParameterType.ShouldBe(typeof(string));
        parameters[4].ParameterType.ShouldBe(typeof(string)); // tenantId (nullable)
        parameters[5].ParameterType.ShouldBe(typeof(string)); // moduleId (nullable)
        parameters[6].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    [Fact]
    public void AssessRiskAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.AssessRiskAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void RequireSupplementaryMeasureAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.RequireSupplementaryMeasureAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void RequireSupplementaryMeasureAsync_ShouldAcceptSupplementaryMeasureType()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.RequireSupplementaryMeasureAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters[1].ParameterType.ShouldBe(typeof(SupplementaryMeasureType));
    }

    [Fact]
    public void SubmitForDPOReviewAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.SubmitForDPOReviewAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void CompleteDPOReviewAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.CompleteDPOReviewAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void GetTIAAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.GetTIAAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, TIAReadModel>>));
    }

    [Fact]
    public void GetTIAByRouteAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ITIAService.GetTIAByRouteAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, TIAReadModel>>));
    }

    [Theory]
    [InlineData(nameof(ITIAService.CreateTIAAsync))]
    [InlineData(nameof(ITIAService.AssessRiskAsync))]
    [InlineData(nameof(ITIAService.RequireSupplementaryMeasureAsync))]
    [InlineData(nameof(ITIAService.SubmitForDPOReviewAsync))]
    [InlineData(nameof(ITIAService.CompleteDPOReviewAsync))]
    [InlineData(nameof(ITIAService.GetTIAAsync))]
    [InlineData(nameof(ITIAService.GetTIAByRouteAsync))]
    public void AllMethods_ShouldReturnValueTaskEither(string methodName)
    {
        var method = InterfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        method.ShouldNotBeNull();
        method.ReturnType.IsGenericType.ShouldBeTrue();
        method.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));

        var innerType = method.ReturnType.GetGenericArguments()[0];
        innerType.IsGenericType.ShouldBeTrue();
        innerType.GetGenericTypeDefinition().ShouldBe(typeof(Either<,>));
    }

    [Theory]
    [InlineData(nameof(ITIAService.CreateTIAAsync))]
    [InlineData(nameof(ITIAService.AssessRiskAsync))]
    [InlineData(nameof(ITIAService.RequireSupplementaryMeasureAsync))]
    [InlineData(nameof(ITIAService.SubmitForDPOReviewAsync))]
    [InlineData(nameof(ITIAService.CompleteDPOReviewAsync))]
    [InlineData(nameof(ITIAService.GetTIAAsync))]
    [InlineData(nameof(ITIAService.GetTIAByRouteAsync))]
    public void AllMethods_ShouldHaveCancellationTokenAsLastParameterWithDefaultValue(string methodName)
    {
        var method = InterfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var parameters = method.GetParameters();
        parameters.Length.ShouldBeGreaterThan(0);

        var lastParam = parameters[^1];
        lastParam.ParameterType.ShouldBe(typeof(CancellationToken));
        lastParam.HasDefaultValue.ShouldBeTrue();
    }
}
