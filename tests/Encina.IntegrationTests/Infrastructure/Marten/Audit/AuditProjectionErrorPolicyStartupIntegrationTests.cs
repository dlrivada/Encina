using Encina.Audit.Marten;
using Encina.Audit.Marten.Projections;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Audit;

/// <summary>
/// Issue #2770 (D22): on a real Marten store built by the container, the audit startup guard sees the application's
/// final projection error flags and stops the host when they skip failing events.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class AuditProjectionErrorPolicyStartupIntegrationTests(MartenFixture fixture)
{
    private ServiceProvider Build(bool appSetsFlags, bool optOut = false, bool appConfiguresAfterAudit = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        void Configure(StoreOptions options)
        {
            options.Connection(fixture.ConnectionString);
            options.DatabaseSchemaName = $"audit_guard_{Guid.NewGuid():N}"[..30];
            options.Events.StreamIdentity = JasperFx.Events.StreamIdentity.AsString;
            if (appSetsFlags)
            {
                options.Projections.Errors.SkipApplyErrors = false;
                options.Projections.Errors.SkipSerializationErrors = false;
                options.Projections.RebuildErrors.SkipApplyErrors = false;
                options.Projections.RebuildErrors.SkipSerializationErrors = false;
            }
        }

        if (!appConfiguresAfterAudit)
        {
            services.AddMarten(Configure);
        }

        services.AddEncinaAuditMarten(o => o.AllowSkippedProjectionErrors = optOut);
        if (appConfiguresAfterAudit)
        {
            services.AddMarten(Configure);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static Task StartingAsync(ServiceProvider provider) =>
        provider.GetServices<IHostedService>().OfType<AuditProjectionErrorPolicyStartupGuard>().Single().StartingAsync(CancellationToken.None);

    private void SkipWhenUnavailable()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("PostgreSQL through Testcontainers is not available.");
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApplicationSetsTheFlags_TheGuardPasses_WhicheverRegistrationComesFirst(bool appConfiguresAfterAudit)
    {
        SkipWhenUnavailable();
        await using var provider = Build(appSetsFlags: true, appConfiguresAfterAudit: appConfiguresAfterAudit);

        await Should.NotThrowAsync(() => StartingAsync(provider));
    }

    [Fact]
    public async Task ApplicationLeavesMartenDefaults_TheHostFailsToStart()
    {
        SkipWhenUnavailable();
        await using var provider = Build(appSetsFlags: false);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => StartingAsync(provider));

        ex.Message.ShouldStartWith(AuditProjectionErrorPolicyStartupGuard.FailureMessage);
    }

    [Fact]
    public async Task ExplicitOptOut_TheHostStartsWithMartenDefaults()
    {
        SkipWhenUnavailable();
        await using var provider = Build(appSetsFlags: false, optOut: true);

        await Should.NotThrowAsync(() => StartingAsync(provider));
    }
}
