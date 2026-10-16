using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Events;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;

using FsCheck;
using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Compliance.DataSubjectRights;

/// <summary>
/// Invariants of the standardised refusal reason (#814).
/// </summary>
public sealed class DSRRejectionReasonPropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private static readonly DSRRejectionReason[] Defined = Enum.GetValues<DSRRejectionReason>();

    private static readonly DSRRejectionReason[] CurrentLaw =
        [.. Defined.Where(r => !r.IsDraftLawGround)];

    // The prefix guarantees a non-blank explanation for any generated text.
    private static string Explanation(NonEmptyString text) => $"x{text.Get}";

    [Property(MaxTest = 100)]
    public bool Deny_ThenReplay_RoundTripsReasonAndExplanation(byte pick, NonEmptyString text)
    {
        var reason = Defined[pick % Defined.Length];
        var explanation = Explanation(text);

        var aggregate = DSRRequestAggregate.Submit(Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);
        aggregate.Deny(reason, explanation, Now.AddHours(1));

        var denied = aggregate.UncommittedEvents.OfType<DSRRequestDenied>().Single();
        var replayed = new DSRRequestAggregate();
        replayed.LoadFromHistory(aggregate.UncommittedEvents);

        return denied.RejectionReason == reason
            && denied.RejectionExplanation == explanation
            && replayed.RejectionReason == reason
            && replayed.RejectionExplanation == explanation
            && replayed.Status == DSRRequestStatus.Rejected;
    }

    [Property(MaxTest = 50)]
    public bool Service_CurrentLawReason_ResultDoesNotDependOnTheOmnibusOption(byte pick, NonEmptyString text)
    {
        var reason = CurrentLaw[pick % CurrentLaw.Length];
        var explanation = Explanation(text);

        var off = DenyAsync(new DataSubjectRightsOptions(), reason, explanation).GetAwaiter().GetResult();
        var on = DenyAsync(
            new DataSubjectRightsOptions { EnableOmnibusArticle12RefusalGrounds = true }, reason, explanation)
            .GetAwaiter().GetResult();

        return off.IsRight && on.IsRight;
    }

    private static async Task<Either<EncinaError, Unit>> DenyAsync(
        DataSubjectRightsOptions options, DSRRejectionReason reason, string explanation)
    {
        var id = Guid.NewGuid();
        var aggregate = DSRRequestAggregate.Submit(id, "subject-1", DataSubjectRight.Access, Now);
        aggregate.ClearUncommittedEvents();

        var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        repository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));
        repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        var service = new DefaultDSRService(
            repository,
            Substitute.For<IReadModelRepository<DSRRequestReadModel>>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            Options.Create(options),
            TimeProvider.System,
            NullLogger<DefaultDSRService>.Instance);

        return await service.DenyRequestAsync(id, reason, explanation);
    }
}
