#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Caching;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Compliance.DPIA.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA.Services;

/// <summary>
/// Proves that approving or rejecting an assessment never writes the acting user id to the log
/// (EventIds 8872 and 8873 and the Debug lines of the same operations).
/// </summary>
public sealed class DefaultDPIAServiceLoggingTests
{
    private const string UserId = "user-4711-secret";
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);

    private sealed class CapturingLogger : ILogger<DefaultDPIAService>
    {
        public List<(LogLevel Level, int EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, eventId.Id, formatter(state, exception)));
    }

    private static (DefaultDPIAService Sut, CapturingLogger Logger) Create()
    {
        var repository = Substitute.For<IAggregateRepository<DPIAAggregate>>();
        var logger = new CapturingLogger();
        var time = new FakeTimeProvider(FixedNow);
        var id = Guid.NewGuid();
        var aggregate = DPIAAggregate.Create(id, "Test.Type", FixedNow);
        aggregate.Evaluate(
            new DPIAResult
            {
                OverallRisk = RiskLevel.Medium,
                IdentifiedRisks = [],
                ProposedMitigations = [],
                RequiresPriorConsultation = false,
                AssessedAtUtc = FixedNow,
            },
            FixedNow);
        repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DPIAAggregate>(aggregate)));
        repository.SaveAsync(Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        var sut = new DefaultDPIAService(
            repository,
            Substitute.For<IReadModelRepository<DPIAReadModel>>(),
            Substitute.For<IDPIAAssessmentEngine>(),
            Substitute.For<IDocumentSession>(),
            Substitute.For<ICacheProvider>(),
            time,
            Options.Create(new DPIAOptions()),
            logger);
        return (sut, logger);
    }

    [Fact]
    public async Task ApproveAssessmentAsync_LogsEventId8872_WithoutTheUserId()
    {
        var (sut, logger) = Create();

        var result = await sut.ApproveAssessmentAsync(Guid.NewGuid(), UserId);

        result.IsRight.ShouldBeTrue();
        logger.Entries.ShouldContain(e => e.EventId == 8872 && e.Level == LogLevel.Information);
        logger.Entries.ShouldAllBe(e => !e.Message.Contains(UserId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RejectAssessmentAsync_LogsEventId8873_WithoutTheUserId()
    {
        var (sut, logger) = Create();

        var result = await sut.RejectAssessmentAsync(Guid.NewGuid(), UserId, "not enough mitigation");

        result.IsRight.ShouldBeTrue();
        logger.Entries.ShouldContain(e => e.EventId == 8873 && e.Level == LogLevel.Information);
        logger.Entries.ShouldAllBe(e => !e.Message.Contains(UserId, StringComparison.Ordinal));
    }
}
