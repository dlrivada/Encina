#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;

using LanguageExt;

namespace Encina.ContractTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Contract tests verifying that <see cref="IApprovedTransferService"/> follows expected API design contracts.
/// </summary>
[Trait("Category", "Contract")]
public class IApprovedTransferServiceContractTests
{
    private static readonly Type InterfaceType = typeof(IApprovedTransferService);

    private static readonly string[] RequiredMethods =
    [
        nameof(IApprovedTransferService.ApproveTransferAsync),
        nameof(IApprovedTransferService.RevokeTransferAsync),
        nameof(IApprovedTransferService.RenewTransferAsync),
        nameof(IApprovedTransferService.GetApprovedTransferAsync),
        nameof(IApprovedTransferService.IsTransferApprovedAsync)
    ];

    [Fact]
    public void IApprovedTransferService_ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IApprovedTransferService_ShouldHaveExactlyFiveMethods()
    {
        var methods = InterfaceType.GetMethods();
        methods.Length.ShouldBe(5);
    }

    [Fact]
    public void IApprovedTransferService_ShouldHaveAllRequiredMethods()
    {
        foreach (var methodName in RequiredMethods)
        {
            var method = InterfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            method.ShouldNotBeNull($"IApprovedTransferService must define {methodName}");
        }
    }

    [Fact]
    public void ApproveTransferAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IApprovedTransferService.ApproveTransferAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Guid>>));
    }

    [Fact]
    public void ApproveTransferAsync_ShouldAcceptTransferBasis()
    {
        var method = InterfaceType.GetMethod(nameof(IApprovedTransferService.ApproveTransferAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters[3].ParameterType.ShouldBe(typeof(TransferBasis));
    }

    [Fact]
    public void RevokeTransferAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IApprovedTransferService.RevokeTransferAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void RenewTransferAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IApprovedTransferService.RenewTransferAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void GetApprovedTransferAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IApprovedTransferService.GetApprovedTransferAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, ApprovedTransferReadModel>>));
    }

    [Fact]
    public void IsTransferApprovedAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IApprovedTransferService.IsTransferApprovedAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, bool>>));
    }

    [Theory]
    [InlineData(nameof(IApprovedTransferService.ApproveTransferAsync))]
    [InlineData(nameof(IApprovedTransferService.RevokeTransferAsync))]
    [InlineData(nameof(IApprovedTransferService.RenewTransferAsync))]
    [InlineData(nameof(IApprovedTransferService.GetApprovedTransferAsync))]
    [InlineData(nameof(IApprovedTransferService.IsTransferApprovedAsync))]
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
    [InlineData(nameof(IApprovedTransferService.ApproveTransferAsync))]
    [InlineData(nameof(IApprovedTransferService.RevokeTransferAsync))]
    [InlineData(nameof(IApprovedTransferService.RenewTransferAsync))]
    [InlineData(nameof(IApprovedTransferService.GetApprovedTransferAsync))]
    [InlineData(nameof(IApprovedTransferService.IsTransferApprovedAsync))]
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
