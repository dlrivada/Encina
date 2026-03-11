#pragma warning disable CA2012 // Use ValueTasks correctly

using System.Reflection;

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAAutoRegistrationHostedService"/>.
/// </summary>
public class DPIAAutoRegistrationHostedServiceTests
{
    private readonly IDPIAStore _store = Substitute.For<IDPIAStore>();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly NullLogger<DPIAAutoRegistrationHostedService> _logger = new();

    #region StartAsync - Attribute Discovery

    [Fact]
    public async Task StartAsync_NoAssemblies_CompletesWithoutErrors()
    {
        var sut = CreateSut([], autoDetect: false);

        await sut.StartAsync(CancellationToken.None);

        await _store.DidNotReceive().SaveAssessmentAsync(
            Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_AssemblyWithAttribute_CreatesDraftAssessment()
    {
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(None)));

        _store.SaveAssessmentAsync(Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        var sut = CreateSut([typeof(TestCommandWithDPIA).Assembly]);

        await sut.StartAsync(CancellationToken.None);

        await _store.Received().SaveAssessmentAsync(
            Arg.Is<DPIAAssessment>(a =>
                a.Status == DPIAAssessmentStatus.Draft &&
                a.RequestTypeName.Contains(nameof(TestCommandWithDPIA))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ExistingAssessment_SkipsSave()
    {
        var existing = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = typeof(TestCommandWithDPIA).FullName!,
            Status = DPIAAssessmentStatus.Approved,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(Some(existing))));

        var sut = CreateSut([typeof(TestCommandWithDPIA).Assembly]);

        await sut.StartAsync(CancellationToken.None);

        await _store.DidNotReceive().SaveAssessmentAsync(
            Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_SaveFails_DoesNotThrow()
    {
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(None)));

        _store.SaveAssessmentAsync(Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                EncinaError.New("Save failed")));

        var sut = CreateSut([typeof(TestCommandWithDPIA).Assembly]);

        var act = () => sut.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region StartAsync - Auto Detection

    [Fact]
    public async Task StartAsync_AutoDetectEnabled_ScansForHighRiskTypes()
    {
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(None)));

        _store.SaveAssessmentAsync(Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        // Uses the current test assembly which has types with high-risk names
        var sut = CreateSut([typeof(TestBiometricHealthCommand).Assembly], autoDetect: true);

        await sut.StartAsync(CancellationToken.None);

        // Should have attempted to save at least the attributed type
        await _store.Received().SaveAssessmentAsync(
            Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region StopAsync

    [Fact]
    public async Task StopAsync_CompletesImmediately()
    {
        var sut = CreateSut([]);

        var act = () => sut.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Draft Assessment Properties

    [Fact]
    public async Task StartAsync_DraftAssessment_HasCorrectTimestamps()
    {
        var fixedTime = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetUtcNow(fixedTime);

        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(None)));

        _store.SaveAssessmentAsync(Arg.Any<DPIAAssessment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        var options = new DPIAOptions
        {
            AutoRegisterFromAttributes = true,
            DefaultReviewPeriod = TimeSpan.FromDays(180)
        };

        var sut = CreateSut([typeof(TestCommandWithDPIA).Assembly], options: options);

        await sut.StartAsync(CancellationToken.None);

        await _store.Received().SaveAssessmentAsync(
            Arg.Is<DPIAAssessment>(a =>
                a.CreatedAtUtc == fixedTime &&
                a.NextReviewAtUtc == fixedTime.AddDays(180) &&
                a.Status == DPIAAssessmentStatus.Draft),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private DPIAAutoRegistrationHostedService CreateSut(
        IReadOnlyList<Assembly> assemblies,
        bool autoDetect = false,
        DPIAOptions? options = null)
    {
        options ??= new DPIAOptions
        {
            AutoRegisterFromAttributes = true,
            AutoDetectHighRisk = autoDetect,
            DefaultReviewPeriod = TimeSpan.FromDays(365)
        };

        var descriptor = new DPIAAutoRegistrationDescriptor(assemblies);

        return new DPIAAutoRegistrationHostedService(
            _store,
            Options.Create(options),
            descriptor,
            _timeProvider,
            _logger);
    }

    #endregion

    #region Test Types

    [RequiresDPIA(ProcessingType = "Testing", Reason = "Unit test type")]
    private sealed class TestCommandWithDPIA;

    private sealed class TestBiometricHealthCommand;

    #endregion
}
