using Encina.Marten;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Core;

public class MartenAggregateRepositoryGuardTests
{
    private static readonly IDocumentSession Session = Substitute.For<IDocumentSession>();
    private static readonly IRequestContext ReqCtx = Substitute.For<IRequestContext>();
    private static readonly ILogger<MartenAggregateRepository<TestAggregate>> Logger = NullLogger<MartenAggregateRepository<TestAggregate>>.Instance;
    private static readonly IOptions<EncinaMartenOptions> Opts = Options.Create(new EncinaMartenOptions());

    #region Constructor Guards

    [Fact]
    public void Constructor_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAggregateRepository<TestAggregate>(null!, ReqCtx, Logger, Opts));

    [Fact]
    public void Constructor_NullRequestContext_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAggregateRepository<TestAggregate>(Session, null!, Logger, Opts));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAggregateRepository<TestAggregate>(Session, ReqCtx, null!, Opts));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenAggregateRepository<TestAggregate>(Session, ReqCtx, Logger, null!));

    #endregion

    #region Method Guards

    [Fact]
    public async Task SaveAsync_NullAggregate_Throws()
    {
        var repo = new MartenAggregateRepository<TestAggregate>(Session, ReqCtx, Logger, Opts);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.SaveAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_NullAggregate_Throws()
    {
        var repo = new MartenAggregateRepository<TestAggregate>(Session, ReqCtx, Logger, Opts);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.CreateAsync(null!));
    }

    #endregion

    public class TestAggregate : global::Encina.DomainModeling.AggregateBase
    {
        protected override void Apply(object domainEvent) { }
    }
}
