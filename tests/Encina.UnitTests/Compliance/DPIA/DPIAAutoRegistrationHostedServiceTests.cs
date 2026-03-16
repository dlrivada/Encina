#pragma warning disable CA2012 // Use ValueTasks correctly

using System.Reflection;

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAAutoRegistrationHostedService"/>.
/// </summary>
public class DPIAAutoRegistrationHostedServiceTests
{
    private readonly IDPIAService _service = Substitute.For<IDPIAService>();
    private readonly NullLogger<DPIAAutoRegistrationHostedService> _logger = new();

    #region StartAsync - Attribute Discovery

    [Fact]
    public async Task StartAsync_NoAssemblies_CompletesWithoutErrors()
    {
        var sut = CreateSut([], autoDetect: false);

        await sut.StartAsync(CancellationToken.None);

        await _service.DidNotReceive().CreateAssessmentAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_AssemblyWithAttribute_CreatesDraftAssessment()
    {
        _service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, DPIAReadModel>(
                DPIAErrors.AssessmentNotFoundByRequestType("unknown"))));

        _service.CreateAssessmentAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(Guid.NewGuid()));

        var sut = CreateSut([typeof(TestCommandWithDPIA).Assembly]);

        await sut.StartAsync(CancellationToken.None);

        await _service.Received().CreateAssessmentAsync(
            Arg.Is<string>(s => s.Contains(nameof(TestCommandWithDPIA))),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ExistingAssessment_SkipsSave()
    {
        var existingReadModel = new DPIAReadModel
        {
            Id = Guid.NewGuid(),
            RequestTypeName = typeof(TestCommandWithDPIA).FullName!,
            Status = DPIAAssessmentStatus.Approved,
        };

        _service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPIAReadModel>(existingReadModel)));

        var sut = CreateSut([typeof(TestCommandWithDPIA).Assembly]);

        await sut.StartAsync(CancellationToken.None);

        await _service.DidNotReceive().CreateAssessmentAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_SaveFails_DoesNotThrow()
    {
        _service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, DPIAReadModel>(
                DPIAErrors.AssessmentNotFoundByRequestType("unknown"))));

        _service.CreateAssessmentAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(
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
        _service.GetAssessmentByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, DPIAReadModel>(
                DPIAErrors.AssessmentNotFoundByRequestType("unknown"))));

        _service.CreateAssessmentAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(Guid.NewGuid()));

        // Uses the current test assembly which has types with high-risk names
        var sut = CreateSut([typeof(TestBiometricHealthCommand).Assembly], autoDetect: true);

        await sut.StartAsync(CancellationToken.None);

        // Should have attempted to create at least the attributed type
        await _service.Received().CreateAssessmentAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
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
            _service,
            Options.Create(options),
            descriptor,
            _logger);
    }

    #endregion

    #region Test Types

    [RequiresDPIA(ProcessingType = "Testing", Reason = "Unit test type")]
    private sealed class TestCommandWithDPIA;

    private sealed class TestBiometricHealthCommand;

    #endregion
}
