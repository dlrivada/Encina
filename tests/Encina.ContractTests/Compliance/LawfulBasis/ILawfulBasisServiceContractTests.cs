#pragma warning disable CA1859 // Contract tests intentionally use interface types

using System.Reflection;

using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.ReadModels;

using LanguageExt;

using GDPRLawfulBasis = Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.ContractTests.Compliance.LawfulBasis;

/// <summary>
/// Contract tests verifying that <see cref="ILawfulBasisService"/> follows expected API design contracts.
/// </summary>
[Trait("Category", "Contract")]
public class ILawfulBasisServiceContractTests
{
    private static readonly Type InterfaceType = typeof(ILawfulBasisService);

    private static readonly string[] RequiredMethods =
    [
        nameof(ILawfulBasisService.RegisterAsync),
        nameof(ILawfulBasisService.ChangeBasisAsync),
        nameof(ILawfulBasisService.RevokeAsync),
        nameof(ILawfulBasisService.CreateLIAAsync),
        nameof(ILawfulBasisService.ApproveLIAAsync),
        nameof(ILawfulBasisService.RejectLIAAsync),
        nameof(ILawfulBasisService.ScheduleLIAReviewAsync),
        nameof(ILawfulBasisService.GetRegistrationAsync),
        nameof(ILawfulBasisService.GetRegistrationByRequestTypeAsync),
        nameof(ILawfulBasisService.GetAllRegistrationsAsync),
        nameof(ILawfulBasisService.GetLIAAsync),
        nameof(ILawfulBasisService.GetLIAByReferenceAsync),
        nameof(ILawfulBasisService.GetPendingLIAReviewsAsync),
        nameof(ILawfulBasisService.HasApprovedLIAAsync)
    ];

    #region Interface Structure

    [Fact]
    public void ILawfulBasisService_ShouldBeInterface()
    {
        InterfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void ILawfulBasisService_ShouldHaveExactlyFourteenMethods()
    {
        var methods = InterfaceType.GetMethods();
        methods.Length.ShouldBe(14);
    }

    [Fact]
    public void ILawfulBasisService_ShouldHaveAllRequiredMethods()
    {
        foreach (var methodName in RequiredMethods)
        {
            var method = InterfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            method.ShouldNotBeNull($"ILawfulBasisService must define {methodName}");
        }
    }

    #endregion

    #region RegisterAsync

    [Fact]
    public void RegisterAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RegisterAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Guid>>));
    }

    [Fact]
    public void RegisterAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RegisterAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(10);
    }

    [Fact]
    public void RegisterAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RegisterAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // id
        parameters[1].ParameterType.ShouldBe(typeof(string)); // requestTypeName
        parameters[2].ParameterType.ShouldBe(typeof(GDPRLawfulBasis)); // basis
        parameters[3].ParameterType.ShouldBe(typeof(string)); // purpose (nullable)
        parameters[4].ParameterType.ShouldBe(typeof(string)); // liaReference (nullable)
        parameters[5].ParameterType.ShouldBe(typeof(string)); // legalReference (nullable)
        parameters[6].ParameterType.ShouldBe(typeof(string)); // contractReference (nullable)
        parameters[7].ParameterType.ShouldBe(typeof(string)); // tenantId (nullable)
        parameters[8].ParameterType.ShouldBe(typeof(string)); // moduleId (nullable)
        parameters[9].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region ChangeBasisAsync

    [Fact]
    public void ChangeBasisAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ChangeBasisAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void ChangeBasisAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ChangeBasisAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(7);
    }

    [Fact]
    public void ChangeBasisAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ChangeBasisAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // registrationId
        parameters[1].ParameterType.ShouldBe(typeof(GDPRLawfulBasis)); // newBasis
        parameters[2].ParameterType.ShouldBe(typeof(string)); // purpose (nullable)
        parameters[3].ParameterType.ShouldBe(typeof(string)); // liaReference (nullable)
        parameters[4].ParameterType.ShouldBe(typeof(string)); // legalReference (nullable)
        parameters[5].ParameterType.ShouldBe(typeof(string)); // contractReference (nullable)
        parameters[6].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region RevokeAsync

    [Fact]
    public void RevokeAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RevokeAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void RevokeAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RevokeAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
    }

    [Fact]
    public void RevokeAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RevokeAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // registrationId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // reason
        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region CreateLIAAsync

    [Fact]
    public void CreateLIAAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.CreateLIAAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Guid>>));
    }

    [Fact]
    public void CreateLIAAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.CreateLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(20);
    }

    [Fact]
    public void CreateLIAAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.CreateLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // id
        parameters[1].ParameterType.ShouldBe(typeof(string)); // reference
        parameters[2].ParameterType.ShouldBe(typeof(string)); // name
        parameters[3].ParameterType.ShouldBe(typeof(string)); // purpose
        parameters[4].ParameterType.ShouldBe(typeof(string)); // legitimateInterest
        parameters[5].ParameterType.ShouldBe(typeof(string)); // benefits
        parameters[6].ParameterType.ShouldBe(typeof(string)); // consequencesIfNotProcessed
        parameters[7].ParameterType.ShouldBe(typeof(string)); // necessityJustification
        parameters[8].ParameterType.ShouldBe(typeof(IReadOnlyList<string>)); // alternativesConsidered
        parameters[9].ParameterType.ShouldBe(typeof(string)); // dataMinimisationNotes
        parameters[10].ParameterType.ShouldBe(typeof(string)); // natureOfData
        parameters[11].ParameterType.ShouldBe(typeof(string)); // reasonableExpectations
        parameters[12].ParameterType.ShouldBe(typeof(string)); // impactAssessment
        parameters[13].ParameterType.ShouldBe(typeof(IReadOnlyList<string>)); // safeguards
        parameters[14].ParameterType.ShouldBe(typeof(string)); // assessedBy
        parameters[15].ParameterType.ShouldBe(typeof(bool)); // dpoInvolvement
        parameters[16].ParameterType.ShouldBe(typeof(string)); // conditions (nullable)
        parameters[17].ParameterType.ShouldBe(typeof(string)); // tenantId (nullable)
        parameters[18].ParameterType.ShouldBe(typeof(string)); // moduleId (nullable)
        parameters[19].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region ApproveLIAAsync

    [Fact]
    public void ApproveLIAAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ApproveLIAAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void ApproveLIAAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ApproveLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(4);
    }

    [Fact]
    public void ApproveLIAAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ApproveLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // liaId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // conclusion
        parameters[2].ParameterType.ShouldBe(typeof(string)); // approvedBy
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region RejectLIAAsync

    [Fact]
    public void RejectLIAAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RejectLIAAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void RejectLIAAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RejectLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(4);
    }

    [Fact]
    public void RejectLIAAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.RejectLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // liaId
        parameters[1].ParameterType.ShouldBe(typeof(string)); // conclusion
        parameters[2].ParameterType.ShouldBe(typeof(string)); // rejectedBy
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region ScheduleLIAReviewAsync

    [Fact]
    public void ScheduleLIAReviewAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ScheduleLIAReviewAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Unit>>));
    }

    [Fact]
    public void ScheduleLIAReviewAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ScheduleLIAReviewAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(4);
    }

    [Fact]
    public void ScheduleLIAReviewAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.ScheduleLIAReviewAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // liaId
        parameters[1].ParameterType.ShouldBe(typeof(DateTimeOffset)); // nextReviewAtUtc
        parameters[2].ParameterType.ShouldBe(typeof(string)); // scheduledBy
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetRegistrationAsync

    [Fact]
    public void GetRegistrationAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetRegistrationAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, LawfulBasisReadModel>>));
    }

    [Fact]
    public void GetRegistrationAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetRegistrationAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
    }

    [Fact]
    public void GetRegistrationAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetRegistrationAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // registrationId
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetRegistrationByRequestTypeAsync

    [Fact]
    public void GetRegistrationByRequestTypeAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetRegistrationByRequestTypeAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>));
    }

    [Fact]
    public void GetRegistrationByRequestTypeAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetRegistrationByRequestTypeAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
    }

    [Fact]
    public void GetRegistrationByRequestTypeAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetRegistrationByRequestTypeAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(string)); // requestTypeName
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetAllRegistrationsAsync

    [Fact]
    public void GetAllRegistrationsAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetAllRegistrationsAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>>));
    }

    [Fact]
    public void GetAllRegistrationsAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetAllRegistrationsAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
    }

    [Fact]
    public void GetAllRegistrationsAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetAllRegistrationsAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetLIAAsync

    [Fact]
    public void GetLIAAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetLIAAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, LIAReadModel>>));
    }

    [Fact]
    public void GetLIAAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
    }

    [Fact]
    public void GetLIAAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(Guid)); // liaId
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetLIAByReferenceAsync

    [Fact]
    public void GetLIAByReferenceAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetLIAByReferenceAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, Option<LIAReadModel>>>));
    }

    [Fact]
    public void GetLIAByReferenceAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetLIAByReferenceAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
    }

    [Fact]
    public void GetLIAByReferenceAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetLIAByReferenceAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(string)); // liaReference
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region GetPendingLIAReviewsAsync

    [Fact]
    public void GetPendingLIAReviewsAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetPendingLIAReviewsAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, IReadOnlyList<LIAReadModel>>>));
    }

    [Fact]
    public void GetPendingLIAReviewsAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetPendingLIAReviewsAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
    }

    [Fact]
    public void GetPendingLIAReviewsAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.GetPendingLIAReviewsAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region HasApprovedLIAAsync

    [Fact]
    public void HasApprovedLIAAsync_ShouldReturnCorrectType()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.HasApprovedLIAAsync));
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ValueTask<Either<EncinaError, bool>>));
    }

    [Fact]
    public void HasApprovedLIAAsync_ShouldHaveCorrectParameterCount()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.HasApprovedLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
    }

    [Fact]
    public void HasApprovedLIAAsync_ShouldHaveCorrectParameterTypes()
    {
        var method = InterfaceType.GetMethod(nameof(ILawfulBasisService.HasApprovedLIAAsync));
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(string)); // liaReference
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    #endregion

    #region Cross-cutting: All Methods Return ValueTask<Either<...>>

    [Theory]
    [InlineData(nameof(ILawfulBasisService.RegisterAsync))]
    [InlineData(nameof(ILawfulBasisService.ChangeBasisAsync))]
    [InlineData(nameof(ILawfulBasisService.RevokeAsync))]
    [InlineData(nameof(ILawfulBasisService.CreateLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.ApproveLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.RejectLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.ScheduleLIAReviewAsync))]
    [InlineData(nameof(ILawfulBasisService.GetRegistrationAsync))]
    [InlineData(nameof(ILawfulBasisService.GetRegistrationByRequestTypeAsync))]
    [InlineData(nameof(ILawfulBasisService.GetAllRegistrationsAsync))]
    [InlineData(nameof(ILawfulBasisService.GetLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.GetLIAByReferenceAsync))]
    [InlineData(nameof(ILawfulBasisService.GetPendingLIAReviewsAsync))]
    [InlineData(nameof(ILawfulBasisService.HasApprovedLIAAsync))]
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
    [InlineData(nameof(ILawfulBasisService.RegisterAsync))]
    [InlineData(nameof(ILawfulBasisService.ChangeBasisAsync))]
    [InlineData(nameof(ILawfulBasisService.RevokeAsync))]
    [InlineData(nameof(ILawfulBasisService.CreateLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.ApproveLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.RejectLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.ScheduleLIAReviewAsync))]
    [InlineData(nameof(ILawfulBasisService.GetRegistrationAsync))]
    [InlineData(nameof(ILawfulBasisService.GetRegistrationByRequestTypeAsync))]
    [InlineData(nameof(ILawfulBasisService.GetAllRegistrationsAsync))]
    [InlineData(nameof(ILawfulBasisService.GetLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.GetLIAByReferenceAsync))]
    [InlineData(nameof(ILawfulBasisService.GetPendingLIAReviewsAsync))]
    [InlineData(nameof(ILawfulBasisService.HasApprovedLIAAsync))]
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

    #region Cross-cutting: EncinaError as Left Type

    [Theory]
    [InlineData(nameof(ILawfulBasisService.RegisterAsync))]
    [InlineData(nameof(ILawfulBasisService.ChangeBasisAsync))]
    [InlineData(nameof(ILawfulBasisService.RevokeAsync))]
    [InlineData(nameof(ILawfulBasisService.CreateLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.ApproveLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.RejectLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.ScheduleLIAReviewAsync))]
    [InlineData(nameof(ILawfulBasisService.GetRegistrationAsync))]
    [InlineData(nameof(ILawfulBasisService.GetRegistrationByRequestTypeAsync))]
    [InlineData(nameof(ILawfulBasisService.GetAllRegistrationsAsync))]
    [InlineData(nameof(ILawfulBasisService.GetLIAAsync))]
    [InlineData(nameof(ILawfulBasisService.GetLIAByReferenceAsync))]
    [InlineData(nameof(ILawfulBasisService.GetPendingLIAReviewsAsync))]
    [InlineData(nameof(ILawfulBasisService.HasApprovedLIAAsync))]
    public void AllMethods_ShouldUseEncinaErrorAsLeftType(string methodName)
    {
        var method = InterfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var eitherType = method.ReturnType.GetGenericArguments()[0];
        var leftType = eitherType.GetGenericArguments()[0];
        leftType.ShouldBe(typeof(EncinaError));
    }

    #endregion
}
