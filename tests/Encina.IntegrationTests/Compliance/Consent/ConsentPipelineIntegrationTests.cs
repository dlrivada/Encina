using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Abstractions;
using Encina.Compliance.Consent.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.IntegrationTests.Compliance.Consent;

/// <summary>
/// Integration tests for the Encina.Compliance.Consent pipeline.
/// Tests DI registration, options validation, and health check integration.
/// </summary>
/// <remarks>
/// <para>
/// After the migration to event-sourced consent (Marten), the full consent flow tests
/// (GrantConsent, WithdrawConsent, ValidateConsent) require a real PostgreSQL + Marten
/// backend and are covered by Marten-specific integration tests.
/// </para>
/// <para>
/// These tests focus on DI wiring and configuration, which do not require a database.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class ConsentPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaConsent_RegistersConsentValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Block;
            options.AutoRegisterFromAttributes = false;
        });

        // Assert — IConsentValidator is registered via TryAdd (descriptor check only;
        // actual resolution requires Marten dependencies which are out of scope here)
        services.ShouldContain(sd => sd.ServiceType == typeof(IConsentValidator));
    }

    [Fact]
    public void AddEncinaConsent_RegistersConsentService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaConsent(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        // Assert — IConsentService is registered via TryAdd (descriptor check only;
        // actual resolution requires Marten dependencies which are out of scope here)
        services.ShouldContain(sd => sd.ServiceType == typeof(IConsentService));
    }

    [Fact]
    public void AddEncinaConsent_WithHealthCheck_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaConsent(options =>
        {
            options.AddHealthCheck = true;
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaConsent_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Warn;
            options.DefaultExpirationDays = 365;
            options.AutoRegisterFromAttributes = false;
            options.DefinePurpose("marketing", p =>
            {
                p.Description = "Marketing communications";
                p.RequiresExplicitOptIn = true;
            });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<ConsentOptions>>().Value;
        options.EnforcementMode.ShouldBe(ConsentEnforcementMode.Warn);
        options.DefaultExpirationDays.ShouldBe(365);
        options.PurposeDefinitions.ShouldContain("marketing");
    }

    [Fact]
    public void AddEncinaConsent_CustomServiceRegisteredBefore_ShouldNotBeOverridden()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var mockService = new FakeConsentService();
        services.AddScoped<IConsentService>(_ => mockService);

        // Act
        services.AddEncinaConsent(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert — pre-registered service should be preserved (TryAdd)
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IConsentService>();
        service.ShouldBeSameAs(mockService);
    }

    [Fact]
    public void AddEncinaConsent_CustomValidatorRegisteredBefore_ShouldNotBeOverridden()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var mockValidator = new FakeConsentValidator();
        services.AddScoped<IConsentValidator>(_ => mockValidator);

        // Act
        services.AddEncinaConsent(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert — pre-registered validator should be preserved (TryAdd)
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IConsentValidator>();
        validator.ShouldBeSameAs(mockValidator);
    }

    #endregion

    #region Options Validation via DI

    [Fact]
    public void AddEncinaConsent_BlockModeNoPurposes_OptionsValidationFails()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Block;
            // No purposes defined — Block mode requires at least one
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        // Act & Assert
        var act = () => provider.GetRequiredService<IOptions<ConsentOptions>>().Value;
        Should.Throw<OptionsValidationException>(act)
            .Message.ShouldContain("PurposeDefinitions");
    }

    [Fact]
    public void AddEncinaConsent_WarnModeNoPurposes_OptionsValidationSucceeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Warn;
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        // Act & Assert — Warn mode allows no purposes
        var options = provider.GetRequiredService<IOptions<ConsentOptions>>().Value;
        options.EnforcementMode.ShouldBe(ConsentEnforcementMode.Warn);
    }

    #endregion

    #region Test Fakes

    /// <summary>
    /// Fake implementation for DI override tests.
    /// </summary>
    private sealed class FakeConsentService : IConsentService
    {
        public ValueTask<Either<EncinaError, Guid>> GrantConsentAsync(
            string dataSubjectId, string purpose, string consentVersionId,
            string source, string grantedBy, string? ipAddress = null,
            string? proofOfConsent = null, IReadOnlyDictionary<string, object?>? metadata = null,
            DateTimeOffset? expiresAtUtc = null, string? tenantId = null,
            string? moduleId = null, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, Guid>(Guid.NewGuid()));

        public ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
            Guid consentId, string withdrawnBy, string? reason = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default));

        public ValueTask<Either<EncinaError, Unit>> RenewConsentAsync(
            Guid consentId, string consentVersionId, string renewedBy,
            DateTimeOffset? newExpiresAtUtc = null, string? source = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default));

        public ValueTask<Either<EncinaError, Unit>> ProvideReconsentAsync(
            Guid consentId, string newConsentVersionId, string source, string grantedBy,
            string? ipAddress = null, string? proofOfConsent = null,
            IReadOnlyDictionary<string, object?>? metadata = null,
            DateTimeOffset? expiresAtUtc = null, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default));

        public ValueTask<Either<EncinaError, ConsentReadModel>> GetConsentAsync(
            Guid consentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, Option<ConsentReadModel>>> GetConsentBySubjectAndPurposeAsync(
            string dataSubjectId, string purpose, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<ConsentReadModel>>> GetAllConsentsAsync(
            string dataSubjectId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
            string dataSubjectId, string purpose, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, bool>(true));

        public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetConsentHistoryAsync(
            Guid consentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Fake validator for DI override tests.
    /// </summary>
    private sealed class FakeConsentValidator : IConsentValidator
    {
        public ValueTask<Either<EncinaError, ConsentValidationResult>> ValidateAsync(
            string subjectId, IEnumerable<string> requiredPurposes,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(
                LanguageExt.Prelude.Right<EncinaError, ConsentValidationResult>(
                    ConsentValidationResult.Valid()));
    }

    #endregion
}
