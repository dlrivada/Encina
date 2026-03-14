#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;

using LanguageExt;

namespace Encina.ContractTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Contract tests verifying that <see cref="ISCCService"/> follows expected API design contracts.
/// </summary>
[Trait("Category", "Contract")]
public class ISCCServiceContractTests
{
    private static readonly Type InterfaceType = typeof(ISCCService);

    private static readonly string[] RequiredMethods =
    [
        nameof(ISCCService.RegisterAgreementAsync),
        nameof(ISCCService.AddSupplementaryMeasureAsync),
        nameof(ISCCService.RevokeAgreementAsync),
        nameof(ISCCService.GetAgreementAsync),
        nameof(ISCCService.ValidateAgreementAsync)
    ];

    [Fact]
    public void ISCCService_ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void ISCCService_ShouldHaveExactlyFiveMethods()
    {
        var methods = InterfaceType.GetMethods();
        methods.Length.ShouldBe(5);
    }

    [Fact]
    public void ISCCService_ShouldHaveAllRequiredMethods()
    {
        foreach (var methodName in RequiredMethods)
        {
            var method = InterfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            method.ShouldNotBeNull($"ISCCService must define {methodName}");
        }
    }

    [Fact]
    public void RegisterAgreementAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ISCCService.RegisterAgreementAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Guid>>));
    }

    [Fact]
    public void RegisterAgreementAsync_ShouldHaveCorrectParameters()
    {
        var method = InterfaceType.GetMethod(nameof(ISCCService.RegisterAgreementAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(8); // processorId, sccModule, version, executedAtUtc, expiresAtUtc, tenantId, moduleId, CancellationToken
        parameters[0].ParameterType.ShouldBe(typeof(string)); // processorId
        parameters[1].ParameterType.ShouldBe(typeof(SCCModule)); // sccModule
        parameters[2].ParameterType.ShouldBe(typeof(string)); // version
        parameters[3].ParameterType.ShouldBe(typeof(DateTimeOffset)); // executedAtUtc
        parameters[4].ParameterType.ShouldBe(typeof(DateTimeOffset?)); // expiresAtUtc
        parameters[5].ParameterType.ShouldBe(typeof(string)); // tenantId (nullable)
        parameters[6].ParameterType.ShouldBe(typeof(string)); // moduleId (nullable)
        parameters[7].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    [Fact]
    public void AddSupplementaryMeasureAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ISCCService.AddSupplementaryMeasureAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void AddSupplementaryMeasureAsync_ShouldAcceptSupplementaryMeasureType()
    {
        var method = InterfaceType.GetMethod(nameof(ISCCService.AddSupplementaryMeasureAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters[1].ParameterType.ShouldBe(typeof(SupplementaryMeasureType));
    }

    [Fact]
    public void RevokeAgreementAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ISCCService.RevokeAgreementAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void GetAgreementAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ISCCService.GetAgreementAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, SCCAgreementReadModel>>));
    }

    [Fact]
    public void ValidateAgreementAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ISCCService.ValidateAgreementAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, SCCValidationResult>>));
    }

    [Theory]
    [InlineData(nameof(ISCCService.RegisterAgreementAsync))]
    [InlineData(nameof(ISCCService.AddSupplementaryMeasureAsync))]
    [InlineData(nameof(ISCCService.RevokeAgreementAsync))]
    [InlineData(nameof(ISCCService.GetAgreementAsync))]
    [InlineData(nameof(ISCCService.ValidateAgreementAsync))]
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
    [InlineData(nameof(ISCCService.RegisterAgreementAsync))]
    [InlineData(nameof(ISCCService.AddSupplementaryMeasureAsync))]
    [InlineData(nameof(ISCCService.RevokeAgreementAsync))]
    [InlineData(nameof(ISCCService.GetAgreementAsync))]
    [InlineData(nameof(ISCCService.ValidateAgreementAsync))]
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
