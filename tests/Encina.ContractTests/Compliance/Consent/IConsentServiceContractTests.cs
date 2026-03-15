#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.Consent.Abstractions;
using Encina.Compliance.Consent.ReadModels;

using LanguageExt;

namespace Encina.ContractTests.Compliance.Consent;

/// <summary>
/// Contract tests verifying that <see cref="IConsentService"/> follows expected API design contracts.
/// </summary>
[Trait("Category", "Contract")]
public class IConsentServiceContractTests
{
    private static readonly Type InterfaceType = typeof(IConsentService);

    private static readonly string[] RequiredMethods =
    [
        nameof(IConsentService.GrantConsentAsync),
        nameof(IConsentService.WithdrawConsentAsync),
        nameof(IConsentService.RenewConsentAsync),
        nameof(IConsentService.ProvideReconsentAsync),
        nameof(IConsentService.GetConsentAsync),
        nameof(IConsentService.GetConsentBySubjectAndPurposeAsync),
        nameof(IConsentService.GetAllConsentsAsync),
        nameof(IConsentService.HasValidConsentAsync),
        nameof(IConsentService.GetConsentHistoryAsync)
    ];

    #region Interface Structure

    [Fact]
    public void IConsentService_ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IConsentService_ShouldHaveExactlyNineMethods()
    {
        var methods = InterfaceType.GetMethods();
        methods.Length.ShouldBe(9);
    }

    [Fact]
    public void IConsentService_ShouldHaveAllRequiredMethods()
    {
        foreach (var methodName in RequiredMethods)
        {
            var method = InterfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            method.ShouldNotBeNull($"IConsentService must define {methodName}");
        }
    }

    #endregion

    #region GrantConsentAsync

    [Fact]
    public void GrantConsentAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GrantConsentAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Guid>>));
    }

    [Fact]
    public void GrantConsentAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GrantConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(12);
    }

    [Fact]
    public void GrantConsentAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GrantConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(string)); // dataSubjectId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // purpose
        parameters[2].ParameterType.ShouldBe(typeof(string)); // consentVersionId
        parameters[3].ParameterType.ShouldBe(typeof(string)); // source
        parameters[4].ParameterType.ShouldBe(typeof(string)); // grantedBy
        parameters[5].ParameterType.ShouldBe(typeof(string)); // ipAddress (nullable)
        parameters[6].ParameterType.ShouldBe(typeof(string)); // proofOfConsent (nullable)
        parameters[7].ParameterType.ShouldBe(typeof(IReadOnlyDictionary<string, object?>)); // metadata (nullable)
        parameters[8].ParameterType.ShouldBe(typeof(DateTimeOffset?)); // expiresAtUtc
        parameters[9].ParameterType.ShouldBe(typeof(string)); // tenantId (nullable)
        parameters[10].ParameterType.ShouldBe(typeof(string)); // moduleId (nullable)
        parameters[11].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region WithdrawConsentAsync

    [Fact]
    public void WithdrawConsentAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.WithdrawConsentAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void WithdrawConsentAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.WithdrawConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(4);
    }

    [Fact]
    public void WithdrawConsentAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.WithdrawConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // consentId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // withdrawnBy
        parameters[2].ParameterType.ShouldBe(typeof(string)); // reason (nullable)
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region RenewConsentAsync

    [Fact]
    public void RenewConsentAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.RenewConsentAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void RenewConsentAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.RenewConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(6);
    }

    [Fact]
    public void RenewConsentAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.RenewConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // consentId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // consentVersionId
        parameters[2].ParameterType.ShouldBe(typeof(string)); // renewedBy
        parameters[3].ParameterType.ShouldBe(typeof(DateTimeOffset?)); // newExpiresAtUtc
        parameters[4].ParameterType.ShouldBe(typeof(string)); // source (nullable)
        parameters[5].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region ProvideReconsentAsync

    [Fact]
    public void ProvideReconsentAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.ProvideReconsentAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void ProvideReconsentAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.ProvideReconsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(9);
    }

    [Fact]
    public void ProvideReconsentAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.ProvideReconsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // consentId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // newConsentVersionId
        parameters[2].ParameterType.ShouldBe(typeof(string)); // source
        parameters[3].ParameterType.ShouldBe(typeof(string)); // grantedBy
        parameters[4].ParameterType.ShouldBe(typeof(string)); // ipAddress (nullable)
        parameters[5].ParameterType.ShouldBe(typeof(string)); // proofOfConsent (nullable)
        parameters[6].ParameterType.ShouldBe(typeof(IReadOnlyDictionary<string, object?>)); // metadata (nullable)
        parameters[7].ParameterType.ShouldBe(typeof(DateTimeOffset?)); // expiresAtUtc
        parameters[8].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetConsentAsync

    [Fact]
    public void GetConsentAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, ConsentReadModel>>));
    }

    [Fact]
    public void GetConsentAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
    }

    [Fact]
    public void GetConsentAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // consentId
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetConsentBySubjectAndPurposeAsync

    [Fact]
    public void GetConsentBySubjectAndPurposeAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentBySubjectAndPurposeAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Option<ConsentReadModel>>>));
    }

    [Fact]
    public void GetConsentBySubjectAndPurposeAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentBySubjectAndPurposeAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
    }

    [Fact]
    public void GetConsentBySubjectAndPurposeAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentBySubjectAndPurposeAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(string)); // dataSubjectId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // purpose
        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetAllConsentsAsync

    [Fact]
    public void GetAllConsentsAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetAllConsentsAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, IReadOnlyList<ConsentReadModel>>>));
    }

    [Fact]
    public void GetAllConsentsAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetAllConsentsAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
    }

    [Fact]
    public void GetAllConsentsAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetAllConsentsAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(string)); // dataSubjectId
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region HasValidConsentAsync

    [Fact]
    public void HasValidConsentAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.HasValidConsentAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, bool>>));
    }

    [Fact]
    public void HasValidConsentAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.HasValidConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
    }

    [Fact]
    public void HasValidConsentAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.HasValidConsentAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(string)); // dataSubjectId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // purpose
        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetConsentHistoryAsync

    [Fact]
    public void GetConsentHistoryAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentHistoryAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, IReadOnlyList<object>>>));
    }

    [Fact]
    public void GetConsentHistoryAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentHistoryAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
    }

    [Fact]
    public void GetConsentHistoryAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(IConsentService.GetConsentHistoryAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // consentId
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region Cross-cutting: All Methods Return ValueTask<Either<...>>

    [Theory]
    [InlineData(nameof(IConsentService.GrantConsentAsync))]
    [InlineData(nameof(IConsentService.WithdrawConsentAsync))]
    [InlineData(nameof(IConsentService.RenewConsentAsync))]
    [InlineData(nameof(IConsentService.ProvideReconsentAsync))]
    [InlineData(nameof(IConsentService.GetConsentAsync))]
    [InlineData(nameof(IConsentService.GetConsentBySubjectAndPurposeAsync))]
    [InlineData(nameof(IConsentService.GetAllConsentsAsync))]
    [InlineData(nameof(IConsentService.HasValidConsentAsync))]
    [InlineData(nameof(IConsentService.GetConsentHistoryAsync))]
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

    #endregion

    #region Cross-cutting: CancellationToken as Last Parameter with Default Value

    [Theory]
    [InlineData(nameof(IConsentService.GrantConsentAsync))]
    [InlineData(nameof(IConsentService.WithdrawConsentAsync))]
    [InlineData(nameof(IConsentService.RenewConsentAsync))]
    [InlineData(nameof(IConsentService.ProvideReconsentAsync))]
    [InlineData(nameof(IConsentService.GetConsentAsync))]
    [InlineData(nameof(IConsentService.GetConsentBySubjectAndPurposeAsync))]
    [InlineData(nameof(IConsentService.GetAllConsentsAsync))]
    [InlineData(nameof(IConsentService.HasValidConsentAsync))]
    [InlineData(nameof(IConsentService.GetConsentHistoryAsync))]
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

    #endregion
}
