using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Evaluators;
using Encina.Compliance.NIS2.Health;
using Encina.Compliance.NIS2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Compliance.NIS2;

#region NIS2CompliancePipelineBehavior

/// <summary>
/// Guard clause tests for <see cref="NIS2CompliancePipelineBehavior{TRequest, TResponse}"/>.
/// Verifies that null arguments are properly rejected in the constructor.
/// </summary>
public sealed class NIS2CompliancePipelineBehaviorGuardTests
{
    private static IMFAEnforcer ValidMfaEnforcer => Substitute.For<IMFAEnforcer>();
    private static ISupplyChainSecurityValidator ValidSupplyChainValidator => Substitute.For<ISupplyChainSecurityValidator>();
    private static IOptions<NIS2Options> ValidOptions => Options.Create(new NIS2Options());
    private static IServiceProvider ValidServiceProvider => Substitute.For<IServiceProvider>();

    private static ILogger<NIS2CompliancePipelineBehavior<TestRequest, string>> ValidLogger =>
        NullLogger<NIS2CompliancePipelineBehavior<TestRequest, string>>.Instance;

    [Fact]
    public void Constructor_NullMfaEnforcer_ThrowsArgumentNullException()
    {
        var act = () => new NIS2CompliancePipelineBehavior<TestRequest, string>(
            null!,
            ValidSupplyChainValidator,
            ValidOptions,
            ValidServiceProvider,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("mfaEnforcer");
    }

    [Fact]
    public void Constructor_NullSupplyChainValidator_ThrowsArgumentNullException()
    {
        var act = () => new NIS2CompliancePipelineBehavior<TestRequest, string>(
            ValidMfaEnforcer,
            null!,
            ValidOptions,
            ValidServiceProvider,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("supplyChainValidator");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new NIS2CompliancePipelineBehavior<TestRequest, string>(
            ValidMfaEnforcer,
            ValidSupplyChainValidator,
            null!,
            ValidServiceProvider,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new NIS2CompliancePipelineBehavior<TestRequest, string>(
            ValidMfaEnforcer,
            ValidSupplyChainValidator,
            ValidOptions,
            null!,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new NIS2CompliancePipelineBehavior<TestRequest, string>(
            ValidMfaEnforcer,
            ValidSupplyChainValidator,
            ValidOptions,
            ValidServiceProvider,
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    private sealed record TestRequest : IRequest<string>;
}

#endregion

#region NIS2ComplianceHealthCheck

/// <summary>
/// Guard clause tests for <see cref="NIS2ComplianceHealthCheck"/>.
/// Verifies that null arguments are properly rejected in the constructor.
/// </summary>
public sealed class NIS2ComplianceHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new NIS2ComplianceHealthCheck(
            null!,
            NullLogger<NIS2ComplianceHealthCheck>.Instance);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new NIS2ComplianceHealthCheck(
            Substitute.For<IServiceProvider>(),
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }
}

#endregion

#region DefaultEncryptionValidator

/// <summary>
/// Guard clause tests for <see cref="DefaultEncryptionValidator"/>.
/// Verifies that null arguments are properly rejected.
/// </summary>
public sealed class DefaultEncryptionValidatorGuardTests
{
    private static readonly IServiceProvider ValidServiceProvider =
        new ServiceCollection().BuildServiceProvider();

    private static readonly ILogger<DefaultEncryptionValidator> ValidEncryptionLogger =
        NullLogger<DefaultEncryptionValidator>.Instance;

    private static DefaultEncryptionValidator CreateValidator() =>
        new(Options.Create(new NIS2Options()), ValidServiceProvider, ValidEncryptionLogger);

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultEncryptionValidator(null!, ValidServiceProvider, ValidEncryptionLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultEncryptionValidator(
            Options.Create(new NIS2Options()), null!, ValidEncryptionLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultEncryptionValidator(
            Options.Create(new NIS2Options()), ValidServiceProvider, null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task IsDataEncryptedAtRestAsync_NullDataCategory_ThrowsArgumentNullException()
    {
        var validator = CreateValidator();

        var act = async () => await validator.IsDataEncryptedAtRestAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public async Task IsDataEncryptedInTransitAsync_NullEndpoint_ThrowsArgumentNullException()
    {
        var validator = CreateValidator();

        var act = async () => await validator.IsDataEncryptedInTransitAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("endpoint");
    }
}

#endregion

#region DefaultSupplyChainSecurityValidator

/// <summary>
/// Guard clause tests for <see cref="DefaultSupplyChainSecurityValidator"/>.
/// Verifies that null arguments are properly rejected.
/// </summary>
public sealed class DefaultSupplyChainSecurityValidatorGuardTests
{
    private static DefaultSupplyChainSecurityValidator CreateValidator() =>
        new(Options.Create(new NIS2Options()), TimeProvider.System);

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultSupplyChainSecurityValidator(null!, TimeProvider.System);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultSupplyChainSecurityValidator(
            Options.Create(new NIS2Options()), null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public async Task AssessSupplierAsync_NullSupplierId_ThrowsArgumentNullException()
    {
        var validator = CreateValidator();

        var act = async () => await validator.AssessSupplierAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("supplierId");
    }

    [Fact]
    public async Task ValidateSupplierForOperationAsync_NullSupplierId_ThrowsArgumentNullException()
    {
        var validator = CreateValidator();

        var act = async () => await validator.ValidateSupplierForOperationAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("supplierId");
    }
}

#endregion

#region NIS2OptionsValidator

/// <summary>
/// Guard clause tests for <see cref="NIS2OptionsValidator"/>.
/// Verifies that null arguments are properly rejected.
/// </summary>
public sealed class NIS2OptionsValidatorGuardTests
{
    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var validator = new NIS2OptionsValidator();

        var act = () => validator.Validate(null, null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }
}

#endregion

#region RiskAnalysisEvaluator

/// <summary>
/// Guard clause tests for <see cref="RiskAnalysisEvaluator"/>.
/// Verifies that null arguments are properly rejected in the constructor.
/// </summary>
public sealed class RiskAnalysisEvaluatorGuardTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RiskAnalysisEvaluator(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }
}

#endregion

#region DefaultNIS2ComplianceValidator

/// <summary>
/// Guard clause tests for <see cref="DefaultNIS2ComplianceValidator"/>.
/// Verifies that null arguments are properly rejected in the constructor.
/// </summary>
public sealed class DefaultNIS2ComplianceValidatorGuardTests
{
    private static IEnumerable<INIS2MeasureEvaluator> ValidEvaluators => Enumerable.Empty<INIS2MeasureEvaluator>();
    private static IOptions<NIS2Options> ValidOptions => Options.Create(new NIS2Options());
    private static IServiceProvider ValidServiceProvider => Substitute.For<IServiceProvider>();

    private static ILogger<DefaultNIS2ComplianceValidator> ValidLogger =>
        NullLogger<DefaultNIS2ComplianceValidator>.Instance;

    [Fact]
    public void Constructor_NullEvaluators_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2ComplianceValidator(
            null!,
            ValidOptions,
            TimeProvider.System,
            ValidServiceProvider,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("evaluators");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2ComplianceValidator(
            ValidEvaluators,
            null!,
            TimeProvider.System,
            ValidServiceProvider,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2ComplianceValidator(
            ValidEvaluators,
            ValidOptions,
            null!,
            ValidServiceProvider,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2ComplianceValidator(
            ValidEvaluators,
            ValidOptions,
            TimeProvider.System,
            null!,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2ComplianceValidator(
            ValidEvaluators,
            ValidOptions,
            TimeProvider.System,
            ValidServiceProvider,
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }
}

#endregion

#region DefaultNIS2IncidentHandler

/// <summary>
/// Guard clause tests for <see cref="DefaultNIS2IncidentHandler"/>.
/// Verifies that null arguments are properly rejected.
/// </summary>
public sealed class DefaultNIS2IncidentHandlerGuardTests
{
    private static IOptions<NIS2Options> ValidOptions => Options.Create(new NIS2Options());

    private static ILogger<DefaultNIS2IncidentHandler> ValidLogger =>
        NullLogger<DefaultNIS2IncidentHandler>.Instance;

    private static readonly IServiceProvider ValidHandlerServiceProvider =
        new ServiceCollection().BuildServiceProvider();

    private static DefaultNIS2IncidentHandler CreateHandler() =>
        new(ValidOptions, TimeProvider.System, ValidHandlerServiceProvider, ValidLogger);

    private static NIS2Incident CreateValidIncident() =>
        NIS2Incident.Create(
            "Test incident",
            NIS2IncidentSeverity.Medium,
            DateTimeOffset.UtcNow,
            isSignificant: true,
            ["service-a"],
            "Initial assessment");

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2IncidentHandler(
            null!,
            TimeProvider.System,
            ValidHandlerServiceProvider,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2IncidentHandler(
            ValidOptions,
            null!,
            ValidHandlerServiceProvider,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2IncidentHandler(
            ValidOptions,
            TimeProvider.System,
            null!,
            ValidLogger);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultNIS2IncidentHandler(
            ValidOptions,
            TimeProvider.System,
            ValidHandlerServiceProvider,
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task ReportIncidentAsync_NullIncident_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();

        var act = async () => await handler.ReportIncidentAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("incident");
    }

    [Fact]
    public async Task IsWithinNotificationDeadlineAsync_NullIncident_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();

        var act = async () => await handler.IsWithinNotificationDeadlineAsync(
            null!, NIS2NotificationPhase.EarlyWarning);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("incident");
    }

    [Fact]
    public async Task GetNextDeadlineAsync_NullIncident_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();

        var act = async () => await handler.GetNextDeadlineAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("incident");
    }
}

#endregion
