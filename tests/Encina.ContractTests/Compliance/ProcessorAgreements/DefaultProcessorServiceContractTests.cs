#pragma warning disable CA2012

using Encina.Caching;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

/// <summary>
/// Contract tests verifying <see cref="DefaultProcessorService"/> behavioral contract:
/// successful registration returns Right with GUID, history returns Left (not yet available).
/// </summary>
[Trait("Category", "Contract")]
public class DefaultProcessorServiceContractTests
{
    private readonly IAggregateRepository<ProcessorAggregate> _repository =
        Substitute.For<IAggregateRepository<ProcessorAggregate>>();

    private readonly IReadModelRepository<ProcessorReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<ProcessorReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();

    [Fact]
    public async Task Contract_RegisterProcessorAsync_WhenRepositorySucceeds_ReturnsRightWithGuid()
    {
        _repository.CreateAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var sut = CreateService();

        var result = await sut.RegisterProcessorAsync(
            "Stripe", "US", "dpo@stripe.com",
            null, 0, SubProcessorAuthorizationType.Specific);

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: id => id.ShouldNotBe(Guid.Empty),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Contract_GetProcessorHistoryAsync_ReturnsLeft_NotYetAvailable()
    {
        var sut = CreateService();

        var result = await sut.GetProcessorHistoryAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetProcessorAsync_WhenNotFound_ReturnsLeft()
    {
        _cache.GetAsync<ProcessorReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ProcessorReadModel?)null);

        _readModelRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ProcessorReadModel>.Left(EncinaError.New("Processor not found")));

        var sut = CreateService();

        var result = await sut.GetProcessorAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    private DefaultProcessorService CreateService()
    {
        return new DefaultProcessorService(
            _repository,
            _readModelRepository,
            _cache,
            TimeProvider.System,
            NullLogger<DefaultProcessorService>.Instance);
    }
}
