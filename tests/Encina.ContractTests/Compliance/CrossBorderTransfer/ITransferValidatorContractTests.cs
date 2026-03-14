#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Contract tests verifying that <see cref="ITransferValidator"/> follows expected API design contracts.
/// </summary>
[Trait("Category", "Contract")]
public class ITransferValidatorContractTests
{
    private static readonly Type InterfaceType = typeof(ITransferValidator);

    [Fact]
    public void ITransferValidator_ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void ITransferValidator_ShouldHaveValidateAsyncMethod()
    {
        var method = InterfaceType.GetMethod("ValidateAsync");
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, TransferValidationOutcome>>));
    }

    [Fact]
    public void ValidateAsync_ShouldHaveTransferRequestParameter()
    {
        var method = InterfaceType.GetMethod("ValidateAsync");
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2); // TransferRequest + CancellationToken
        parameters[0].ParameterType.ShouldBe(typeof(TransferRequest));
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    [Fact]
    public void ValidateAsync_CancellationToken_ShouldHaveDefaultValue()
    {
        var method = InterfaceType.GetMethod("ValidateAsync");
        method.ShouldNotBeNull();
        var ctParam = method.GetParameters()[1];
        ctParam.HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void ITransferValidator_ShouldHaveExactlyOneMethod()
    {
        var methods = InterfaceType.GetMethods();
        methods.Length.ShouldBe(1);
    }
}
