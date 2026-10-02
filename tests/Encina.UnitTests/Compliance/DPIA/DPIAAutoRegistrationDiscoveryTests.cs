#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using System.Reflection;

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Tests the discovery and registration decisions of <see cref="DPIAAutoRegistrationHostedService"/>
/// over a controlled set of types, including every failure path.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "DPIA")]
public sealed class DPIAAutoRegistrationDiscoveryTests
{
    private const string SentinelMessage = "SENTINEL-lookup-failure-message";

    private readonly IDPIAService _service = Substitute.For<IDPIAService>();
    private readonly ComplianceLogCapture<DPIAAutoRegistrationHostedService> _logger = new();

    [RequiresDPIA(ProcessingType = "Scoring", Reason = "Explicit reason")]
    private sealed class AttributedScoringCommand;

    [RequiresDPIA]
    private sealed class AttributedBiometricHealthCommand;

    private sealed class BiometricHealthEnrollmentCommand;

    private sealed class PlainCommand;

    private sealed class FakeAssembly(params Type[] types) : Assembly
    {
        public override Type[] GetTypes() => types;
    }

    private sealed class UnloadableAssembly : Assembly
    {
        public override Type[] GetTypes() => throw new ReflectionTypeLoadException([], []);
    }

    private DPIAAutoRegistrationHostedService CreateSut(bool autoDetect, params Assembly[] assemblies)
        => new(
            _service,
            Options.Create(new DPIAOptions { AutoDetectHighRisk = autoDetect }),
            new DPIAAutoRegistrationDescriptor(assemblies),
            _logger);

    private void SetupLookup(Either<EncinaError, DPIAReadModel> result)
        => _service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(result));

    private void SetupCreate(Either<EncinaError, Guid> result)
        => _service.CreateAssessmentAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(result));

    private static Either<EncinaError, DPIAReadModel> NotFound()
        => Left<EncinaError, DPIAReadModel>(DPIAErrors.AssessmentNotFoundByRequestType("unknown"));

    private IEnumerable<string> CreatedTypeNames()
        => _service.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IDPIAService.CreateAssessmentAsync))
            .Select(c => (string)c.GetArguments()[0]!);

    [Fact]
    public async Task StartAsync_NotFound_CreatesDraftWithAttributeValues()
    {
        SetupLookup(NotFound());
        SetupCreate(Guid.NewGuid());
        var sut = CreateSut(autoDetect: false, new FakeAssembly(typeof(AttributedScoringCommand), typeof(PlainCommand)));

        await sut.StartAsync(CancellationToken.None);

        await _service.Received(1).CreateAssessmentAsync(
            typeof(AttributedScoringCommand).FullName!,
            "Scoring",
            "Explicit reason",
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        _logger.Entries.ShouldContain(e => e.Message.Contains("RegisteredCount=1, SkippedCount=0"));
    }

    [Fact]
    public async Task StartAsync_AttributeWithoutReason_UsesDefaultReason()
    {
        SetupLookup(NotFound());
        SetupCreate(Guid.NewGuid());
        var sut = CreateSut(autoDetect: false, new FakeAssembly(typeof(AttributedBiometricHealthCommand)));

        await sut.StartAsync(CancellationToken.None);

        await _service.Received(1).CreateAssessmentAsync(
            typeof(AttributedBiometricHealthCommand).FullName!,
            Arg.Any<string?>(),
            "Auto-registered at startup.",
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_AssemblyThatCannotLoadTypes_IsSkippedAndTheRestIsProcessed()
    {
        SetupLookup(NotFound());
        SetupCreate(Guid.NewGuid());
        var sut = CreateSut(
            autoDetect: true,
            new UnloadableAssembly(),
            new FakeAssembly(typeof(AttributedScoringCommand)));

        await sut.StartAsync(CancellationToken.None);

        CreatedTypeNames().ShouldBe([typeof(AttributedScoringCommand).FullName!]);
    }

    [Fact]
    public async Task StartAsync_AutoDetect_AddsHeuristicTypesOnceAndKeepsAttributedOnes()
    {
        SetupLookup(NotFound());
        SetupCreate(Guid.NewGuid());
        var sut = CreateSut(
            autoDetect: true,
            new FakeAssembly(
                typeof(AttributedBiometricHealthCommand),
                typeof(BiometricHealthEnrollmentCommand),
                typeof(PlainCommand)));

        await sut.StartAsync(CancellationToken.None);

        CreatedTypeNames().Order().ShouldBe(
            new[]
            {
                typeof(AttributedBiometricHealthCommand).FullName!,
                typeof(BiometricHealthEnrollmentCommand).FullName!
            }.Order());
    }

    [Fact]
    public async Task StartAsync_AutoDetectDisabled_IgnoresHeuristicTypes()
    {
        SetupLookup(NotFound());
        SetupCreate(Guid.NewGuid());
        var sut = CreateSut(autoDetect: false, new FakeAssembly(typeof(BiometricHealthEnrollmentCommand)));

        await sut.StartAsync(CancellationToken.None);

        CreatedTypeNames().ShouldBeEmpty();
    }

    [Fact]
    public async Task StartAsync_ExistingAssessment_IsCountedAsSkipped()
    {
        SetupLookup(Right<EncinaError, DPIAReadModel>(new DPIAReadModel
        {
            Id = Guid.NewGuid(),
            RequestTypeName = typeof(AttributedScoringCommand).FullName!,
            Status = DPIAAssessmentStatus.Approved
        }));
        var sut = CreateSut(autoDetect: false, new FakeAssembly(typeof(AttributedScoringCommand)));

        await sut.StartAsync(CancellationToken.None);

        CreatedTypeNames().ShouldBeEmpty();
        _logger.Entries.ShouldContain(e => e.Message.Contains("RegisteredCount=0, SkippedCount=1"));
    }

    [Fact]
    public async Task StartAsync_LookupFailsWithOtherError_SkipsTypeAndLogsWarning()
    {
        SetupLookup(Left<EncinaError, DPIAReadModel>(EncinaErrors.Create("dpia.store_error", SentinelMessage)));
        var sut = CreateSut(autoDetect: false, new FakeAssembly(typeof(AttributedScoringCommand)));

        await sut.StartAsync(CancellationToken.None);

        CreatedTypeNames().ShouldBeEmpty();
        var warning = _logger.Entries.Single(e => e.Level == LogLevel.Warning);
        warning.Exception.ShouldBeOfType<InvalidOperationException>();
        warning.Exception!.Message.ShouldNotContain(SentinelMessage);
        _logger.Entries.ShouldAllBe(e => !e.Message.Contains(SentinelMessage));
        _logger.Entries.ShouldContain(e => e.Message.Contains("RegisteredCount=0, SkippedCount=0"));
    }

    [Fact]
    public async Task StartAsync_LookupThrows_LogsRedactedExceptionAndContinuesWithNextType()
    {
        _service.GetAssessmentByRequestTypeAsync(
                typeof(AttributedScoringCommand).FullName!, Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, DPIAReadModel>>>(_ => throw new InvalidOperationException(SentinelMessage));
        _service.GetAssessmentByRequestTypeAsync(
                typeof(AttributedBiometricHealthCommand).FullName!, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(NotFound()));
        SetupCreate(Guid.NewGuid());
        var sut = CreateSut(
            autoDetect: false,
            new FakeAssembly(typeof(AttributedScoringCommand), typeof(AttributedBiometricHealthCommand)));

        await sut.StartAsync(CancellationToken.None);

        CreatedTypeNames().ShouldBe([typeof(AttributedBiometricHealthCommand).FullName!]);
        var failure = _logger.Entries.Single(e => e.Exception is not null);
        failure.Level.ShouldBe(LogLevel.Warning);
        failure.Exception.ShouldBeOfType<RedactedException>();
        failure.Exception!.Message.ShouldNotContain(SentinelMessage);
        _logger.Entries.ShouldAllBe(e => !e.Message.Contains(SentinelMessage));
        _logger.Entries.ShouldContain(e => e.Message.Contains("RegisteredCount=1, SkippedCount=0"));
    }

    [Fact]
    public async Task StartAsync_CreateReturnsError_LogsErrorCodeOnlyAndDoesNotCountRegistration()
    {
        SetupLookup(NotFound());
        SetupCreate(Left<EncinaError, Guid>(EncinaErrors.Create("dpia.save_failed", SentinelMessage)));
        var sut = CreateSut(autoDetect: false, new FakeAssembly(typeof(AttributedScoringCommand)));

        await sut.StartAsync(CancellationToken.None);

        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("dpia.save_failed"));
        _logger.Entries.ShouldAllBe(e => !e.Message.Contains(SentinelMessage));
        _logger.Entries.ShouldContain(e => e.Message.Contains("RegisteredCount=0, SkippedCount=0"));
    }
}
