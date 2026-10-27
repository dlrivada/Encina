using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.ReadModels;
using Encina.Compliance.Consent.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Compliance.Consent;

/// <summary>
/// Guard tests for the consent gates that fail closed: <see cref="DefaultConsentService"/> denies every
/// operation when tenant isolation is required and no tenant is present (SPEC-002 DEC-006), and the
/// registration, options and attribute entry points reject invalid arguments.
/// </summary>
public class ConsentFailClosedGuardTests
{
    private const string Subject = "subject-1";
    private const string Purpose = "marketing";

    private readonly IAggregateRepository<ConsentAggregate> _repository = Substitute.For<IAggregateRepository<ConsentAggregate>>();
    private readonly IReadModelRepository<ConsentReadModel> _readModelRepository = Substitute.For<IReadModelRepository<ConsentReadModel>>();
    private readonly IRequestContextAccessor _requestContextAccessor = Substitute.For<IRequestContextAccessor>();

    private DefaultConsentService CreateService(bool? requireTenantContext = true) =>
        new(
            _repository,
            _readModelRepository,
            Substitute.For<ICacheProvider>(),
            TimeProvider.System,
            _requestContextAccessor,
            Options.Create(new ConsentOptions { RequireTenantContext = requireTenantContext }),
            NullLogger<DefaultConsentService>.Instance);

    public static TheoryData<string> Operations => new()
    {
        "GrantConsent",
        "WithdrawConsent",
        "RenewConsent",
        "ProvideReconsent",
        "GetConsent",
        "GetConsentBySubjectAndPurpose",
        "GetAllConsents",
        "HasValidConsent"
    };

    private static ValueTask<Either<EncinaError, bool>> Invoke(DefaultConsentService service, string operation)
    {
        var id = Guid.NewGuid();
        return operation switch
        {
            "GrantConsent" => Map(service.GrantConsentAsync(Subject, Purpose, "v1", "web", "actor")),
            "WithdrawConsent" => Map(service.WithdrawConsentAsync(id, "actor")),
            "RenewConsent" => Map(service.RenewConsentAsync(id, "v2", "actor")),
            "ProvideReconsent" => Map(service.ProvideReconsentAsync(id, "v2", "web", "actor")),
            "GetConsent" => Map(service.GetConsentAsync(id)),
            "GetConsentBySubjectAndPurpose" => Map(service.GetConsentBySubjectAndPurposeAsync(Subject, Purpose)),
            "GetAllConsents" => Map(service.GetAllConsentsAsync(Subject)),
            "HasValidConsent" => Map(service.HasValidConsentAsync(Subject, Purpose)),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    private static async ValueTask<Either<EncinaError, bool>> Map<T>(ValueTask<Either<EncinaError, T>> pending)
    {
        var result = await pending;
        return result.Map(_ => true);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Operation_NoTenantAndTenantRequired_FailsClosedWithoutTouchingTheStore(string operation)
    {
        _requestContextAccessor.RequestContext.Returns((IRequestContext?)null);
        var service = CreateService(requireTenantContext: true);

        var result = await Invoke(service, operation);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => string.Empty, error => error.GetCode().IfNone(string.Empty))
            .ShouldBe(ConsentErrors.TenantRequiredCode);
        _repository.ReceivedCalls().ShouldBeEmpty();
        _readModelRepository.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task GrantConsent_ExplicitTenantDiffersFromAmbient_FailsClosedWithoutTouchingTheStore()
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns("tenant-a");
        _requestContextAccessor.RequestContext.Returns(context);
        var service = CreateService(requireTenantContext: false);

        var result = await service.GrantConsentAsync(Subject, Purpose, "v1", "web", "actor", tenantId: "tenant-b");

        result.Match(_ => string.Empty, error => error.GetCode().IfNone(string.Empty))
            .ShouldBe(ConsentErrors.TenantRequiredCode);
        _repository.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void AddEncinaConsent_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaConsent();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddConsentAggregates_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddConsentAggregates();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void OptionsValidator_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ConsentOptionsValidator().Validate(null, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void SubjectIdConversion_NullProperty_ThrowsArgumentNullException()
    {
        var act = () => SubjectIdConversion.ToInvariantString("subject", null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("property");
    }

    [Fact]
    public void SubjectIdConversion_UnsupportedSubjectType_ThrowsInvalidOperationException()
    {
        var property = typeof(ConsentOptions).GetProperty(nameof(ConsentOptions.EnforcementMode))!;

        var act = () => SubjectIdConversion.ToInvariantString(ConsentEnforcementMode.Block, property);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RequireConsentAttribute_NullPurposes_ThrowsArgumentNullException()
    {
        var act = () => new RequireConsentAttribute(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("purposes");
    }

    [Fact]
    public void RequireConsentAttribute_NoPurposes_ThrowsArgumentException()
    {
        var act = () => new RequireConsentAttribute();

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("purposes");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DefinePurpose_BlankPurpose_ThrowsArgumentException(string? purpose)
    {
        var options = new ConsentOptions();

        var act = () => options.DefinePurpose(purpose!);

        Should.Throw<ArgumentException>(act);
    }
}
