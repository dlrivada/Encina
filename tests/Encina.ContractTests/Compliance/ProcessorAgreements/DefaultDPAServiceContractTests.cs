#pragma warning disable CA2012

using Encina.Caching;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

/// <summary>
/// Contract tests verifying <see cref="DefaultDPAService"/> behavioral contract:
/// successful execution returns Right with new GUID, query operations return expected results.
/// </summary>
[Trait("Category", "Contract")]
public class DefaultDPAServiceContractTests
{
    private readonly IAggregateRepository<DPAAggregate> _repository =
        Substitute.For<IAggregateRepository<DPAAggregate>>();

    private readonly IReadModelRepository<DPAReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<DPAReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly IOptions<ProcessorAgreementOptions> _options = Options.Create(new ProcessorAgreementOptions());

    private static readonly DPAMandatoryTerms FullyCompliantTerms = new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    [Fact]
    public async Task Contract_ExecuteDPAAsync_WhenRepositorySucceeds_ReturnsRightWithGuid()
    {
        _repository.CreateAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var sut = CreateService();

        var result = await sut.ExecuteDPAAsync(
            Guid.NewGuid(), FullyCompliantTerms, true,
            ["purpose"], DateTimeOffset.UtcNow, null);

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: id => id.ShouldNotBe(Guid.Empty),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Contract_GetDPAHistoryAsync_ReturnsLeft_NotYetAvailable()
    {
        var sut = CreateService();

        var result = await sut.GetDPAHistoryAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetDPAAsync_WhenNotFound_ReturnsLeft()
    {
        _cache.GetAsync<DPAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DPAReadModel?)null);

        _readModelRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, DPAReadModel>.Left(EncinaError.New("DPA not found")));

        var sut = CreateService();

        var result = await sut.GetDPAAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    private DefaultDPAService CreateService()
    {
        return new DefaultDPAService(
            _repository,
            _readModelRepository,
            _cache,
            TimeProvider.System,
            _options,
            NullLogger<DefaultDPAService>.Instance);
    }
}
