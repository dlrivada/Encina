using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// Registration of the decision audit recorder, reader and options validator (#751 Phase 3): the
/// provider builds under <c>ValidateOnBuild</c> and <c>ValidateScopes</c> with and without an
/// operation audit store, nothing resolves the store at construction, and invalid bounds fail at start.
/// </summary>
public sealed class ABACDecisionAuditRegistrationTests
{
    private static readonly ServiceProviderOptions Validated = new() { ValidateOnBuild = true, ValidateScopes = true };

    [Fact]
    public async Task AddEncinaABAC_WithoutAnOperationAuditStore_BuildsAndTheReaderReportsTheStoreUnavailable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaABAC();

        await using var provider = services.BuildServiceProvider(Validated);
        await using var scope = provider.CreateAsyncScope();

        provider.GetRequiredService<IABACDecisionRecorder>().ShouldBeOfType<AuditStoreABACDecisionRecorder>();
        var reader = scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>();
        var result = await reader.QueryAsync(new ABACDecisionAuditQuery());
        result.Match(Right: _ => "<right>", Left: e => e.GetCode().IfNone("")).ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode);
    }

    [Fact]
    public async Task AddEncinaABAC_WithAScopedStore_TheSingletonRecorderWritesAndTheScopedReaderReadsBack()
    {
        var store = new InMemoryOperationAuditStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IOperationAuditStore>(_ => store);
        services.AddEncinaABAC();

        await using var provider = services.BuildServiceProvider(Validated);
        var record = new ABACDecisionRecord
        {
            DecisionId = Guid.CreateVersion7(),
            UserId = "user-1",
            IdentityKind = IdentityKind.User,
            CorrelationId = "corr-1",
            RequestType = "GetOrderQuery",
            EnforcedOutcome = ABACEnforcedOutcome.Denied,
            ReasonCode = ABACErrors.AccessDeniedCode,
            EnforcementMode = ABACEnforcementMode.Block,
            StartedAtUtc = DateTimeOffset.UnixEpoch,
            CompletedAtUtc = DateTimeOffset.UnixEpoch
        };

        (await provider.GetRequiredService<IABACDecisionRecorder>().RecordAsync(record)).IsRight.ShouldBeTrue();

        await using var scope = provider.CreateAsyncScope();
        var page = await scope.ServiceProvider.GetRequiredService<IABACDecisionAuditReader>().QueryAsync(new ABACDecisionAuditQuery());
        page.Match(Right: p => p.Items.Single().DecisionId, Left: _ => Guid.Empty).ShouldBe(record.DecisionId);
    }

    [Fact]
    public void AddEncinaABAC_InvalidWriteTimeout_FailsWhenTheOptionsAreResolved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaABAC(options => options.DecisionAudit.WriteTimeout = TimeSpan.Zero);

        using var provider = services.BuildServiceProvider(Validated);

        var ex = Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ABACOptions>>().Value);
        ex.Message.ShouldContain("WriteTimeout");
    }

    [Theory]
    [InlineData(0, 5, "WriteTimeout")]
    [InlineData(-1, 5, "WriteTimeout")]
    [InlineData(5, 0, "MaxTraceEntries")]
    public void ABACOptionsValidator_RejectsUnusableBounds(int timeoutSeconds, int maxTraceEntries, string named)
    {
        var options = new ABACOptions();
        options.DecisionAudit.WriteTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        options.DecisionAudit.MaxTraceEntries = maxTraceEntries;

        var result = new ABACOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(named);
    }

    [Fact]
    public void ABACOptionsValidator_RejectsATimeoutAboveWhatATokenSourceAccepts()
    {
        var options = new ABACOptions();
        options.DecisionAudit.WriteTimeout = TimeSpan.FromDays(30);

        new ABACOptionsValidator().Validate(null, options).Failed.ShouldBeTrue();
    }

    [Fact]
    public void ABACOptionsValidator_AcceptsTheDefaults()
    {
        var options = new ABACOptions();

        options.DecisionAudit.WriteTimeout.ShouldBe(TimeSpan.FromSeconds(5));
        options.DecisionAudit.AllowCrossTenantQueries.ShouldBeFalse();
        new ABACOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ABACOptionsValidator_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACOptionsValidator().Validate(null, null!));
}
