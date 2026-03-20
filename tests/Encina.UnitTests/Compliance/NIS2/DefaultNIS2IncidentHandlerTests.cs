#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Model;
using Encina.Testing.Time;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="DefaultNIS2IncidentHandler"/>.
/// </summary>
public class DefaultNIS2IncidentHandlerTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly DateTimeOffset _baseTime = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    public DefaultNIS2IncidentHandlerTests()
    {
        _timeProvider = new FakeTimeProvider(_baseTime);
    }

    private DefaultNIS2IncidentHandler CreateSut(
        NIS2Options? options = null,
        IServiceProvider? serviceProvider = null) =>
        new(
            Options.Create(options ?? new NIS2Options()),
            _timeProvider,
            serviceProvider ?? new ServiceCollection().BuildServiceProvider(),
            NullLogger<DefaultNIS2IncidentHandler>.Instance);

    private NIS2Incident CreateIncident(
        DateTimeOffset? detectedAt = null,
        DateTimeOffset? earlyWarningAt = null,
        DateTimeOffset? incidentNotificationAt = null,
        DateTimeOffset? finalReportAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Description = "Test incident",
            Severity = NIS2IncidentSeverity.High,
            DetectedAtUtc = detectedAt ?? _baseTime,
            IsSignificant = true,
            AffectedServices = ["ServiceA", "ServiceB"],
            InitialAssessment = "Initial assessment of the incident.",
            EarlyWarningAtUtc = earlyWarningAt,
            IncidentNotificationAtUtc = incidentNotificationAt,
            FinalReportAtUtc = finalReportAt
        };

    #region ReportIncidentAsync

    [Fact]
    public async Task ReportIncidentAsync_ValidIncident_ShouldReturnSuccess()
    {
        // Arrange
        var sut = CreateSut();
        var incident = CreateIncident();

        // Act
        var result = await sut.ReportIncidentAsync(incident);

        // Assert
        result.IsRight.Should().BeTrue();
        var unit = result.Match(r => r, _ => default);
        unit.Should().Be(Unit.Default);
    }

    #endregion

    #region IsWithinNotificationDeadlineAsync

    [Fact]
    public async Task IsWithinNotificationDeadlineAsync_EarlyWarning_Within24Hours_ShouldReturnTrue()
    {
        // Arrange
        var sut = CreateSut();
        var incident = CreateIncident(detectedAt: _baseTime);

        // Advance time 12 hours (within the 24h early warning window)
        _timeProvider.Advance(TimeSpan.FromHours(12));

        // Act
        var result = await sut.IsWithinNotificationDeadlineAsync(
            incident, NIS2NotificationPhase.EarlyWarning);

        // Assert
        result.IsRight.Should().BeTrue();
        var isWithin = result.Match(r => r, _ => false);
        isWithin.Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinNotificationDeadlineAsync_EarlyWarning_After24Hours_ShouldReturnFalse()
    {
        // Arrange
        var sut = CreateSut();
        var incident = CreateIncident(detectedAt: _baseTime);

        // Advance time 25 hours (past the 24h early warning window)
        _timeProvider.Advance(TimeSpan.FromHours(25));

        // Act
        var result = await sut.IsWithinNotificationDeadlineAsync(
            incident, NIS2NotificationPhase.EarlyWarning);

        // Assert
        result.IsRight.Should().BeTrue();
        var isWithin = result.Match(r => r, _ => true);
        isWithin.Should().BeFalse();
    }

    [Fact]
    public async Task IsWithinNotificationDeadlineAsync_IncidentNotification_Within72Hours_ShouldReturnTrue()
    {
        // Arrange
        var sut = CreateSut();
        var incident = CreateIncident(detectedAt: _baseTime);

        // Advance time 48 hours (within the 72h notification window)
        _timeProvider.Advance(TimeSpan.FromHours(48));

        // Act
        var result = await sut.IsWithinNotificationDeadlineAsync(
            incident, NIS2NotificationPhase.IncidentNotification);

        // Assert
        result.IsRight.Should().BeTrue();
        var isWithin = result.Match(r => r, _ => false);
        isWithin.Should().BeTrue();
    }

    #endregion

    #region GetNextDeadlineAsync

    [Fact]
    public async Task GetNextDeadlineAsync_NoPhasesDone_ShouldReturnEarlyWarning()
    {
        // Arrange
        var sut = CreateSut();
        var incident = CreateIncident();

        // Act
        var result = await sut.GetNextDeadlineAsync(incident);

        // Assert
        result.IsRight.Should().BeTrue();
        var (phase, deadline) = result.Match(r => r, _ => default);
        phase.Should().Be(NIS2NotificationPhase.EarlyWarning);
        deadline.Should().Be(incident.EarlyWarningDeadlineUtc);
    }

    [Fact]
    public async Task GetNextDeadlineAsync_EarlyWarningDone_ShouldReturnIncidentNotification()
    {
        // Arrange
        var sut = CreateSut();
        var incident = CreateIncident(
            earlyWarningAt: _baseTime.AddHours(12));

        // Act
        var result = await sut.GetNextDeadlineAsync(incident);

        // Assert
        result.IsRight.Should().BeTrue();
        var (phase, deadline) = result.Match(r => r, _ => default);
        phase.Should().Be(NIS2NotificationPhase.IncidentNotification);
        deadline.Should().Be(incident.IncidentNotificationDeadlineUtc);
    }

    [Fact]
    public async Task GetNextDeadlineAsync_AllPhasesDone_ShouldReturnError()
    {
        // Arrange
        var sut = CreateSut();
        var notificationTime = _baseTime.AddHours(48);
        var incident = CreateIncident(
            earlyWarningAt: _baseTime.AddHours(12),
            incidentNotificationAt: notificationTime,
            finalReportAt: notificationTime.AddDays(15));

        // Act
        var result = await sut.GetNextDeadlineAsync(incident);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.Message.Should().Contain("completed");
    }

    #endregion

    #region ReportIncidentAsync — Breach Notification Forwarding

    [Fact]
    public async Task ReportIncidentAsync_BreachServiceRegistered_ShouldForwardIncident()
    {
        // Arrange — register IBreachNotificationService mock
        var breachService = Substitute.For<IBreachNotificationService>();
        breachService.RecordBreachAsync(
                Arg.Any<string>(), Arg.Any<BreachSeverity>(), Arg.Any<string>(),
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Guid>>(
                Right<EncinaError, Guid>(Guid.NewGuid())));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IBreachNotificationService)).Returns(breachService);

        var sut = CreateSut(serviceProvider: sp);
        var incident = CreateIncident();

        // Act
        var result = await sut.ReportIncidentAsync(incident);

        // Assert
        result.IsRight.Should().BeTrue();
        await breachService.Received(1).RecordBreachAsync(
            Arg.Is<string>(s => s.Contains("NIS2 Incident")),
            Arg.Any<BreachSeverity>(),
            Arg.Is("NIS2IncidentHandler"),
            Arg.Is(0),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportIncidentAsync_NoBreachService_ShouldStillSucceed()
    {
        // Arrange — no IBreachNotificationService registered
        var sut = CreateSut();
        var incident = CreateIncident();

        // Act
        var result = await sut.ReportIncidentAsync(incident);

        // Assert — should succeed without forwarding
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task ReportIncidentAsync_BreachServiceThrows_ShouldStillSucceed()
    {
        // Arrange — IBreachNotificationService throws exception
        var breachService = Substitute.For<IBreachNotificationService>();
        breachService.RecordBreachAsync(
                Arg.Any<string>(), Arg.Any<BreachSeverity>(), Arg.Any<string>(),
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Breach service down"));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IBreachNotificationService)).Returns(breachService);

        var sut = CreateSut(serviceProvider: sp);
        var incident = CreateIncident();

        // Act — resilience should catch the exception
        var result = await sut.ReportIncidentAsync(incident);

        // Assert — incident report still succeeds
        result.IsRight.Should().BeTrue();
    }

    [Theory]
    [InlineData(NIS2IncidentSeverity.Critical, BreachSeverity.Critical)]
    [InlineData(NIS2IncidentSeverity.High, BreachSeverity.High)]
    [InlineData(NIS2IncidentSeverity.Medium, BreachSeverity.Medium)]
    [InlineData(NIS2IncidentSeverity.Low, BreachSeverity.Low)]
    public async Task ReportIncidentAsync_ShouldMapSeverityCorrectly(
        NIS2IncidentSeverity nis2Severity,
        BreachSeverity expectedBreachSeverity)
    {
        // Arrange
        var breachService = Substitute.For<IBreachNotificationService>();
        breachService.RecordBreachAsync(
                Arg.Any<string>(), Arg.Any<BreachSeverity>(), Arg.Any<string>(),
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Guid>>(
                Right<EncinaError, Guid>(Guid.NewGuid())));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IBreachNotificationService)).Returns(breachService);

        var sut = CreateSut(serviceProvider: sp);
        var incident = new NIS2Incident
        {
            Id = Guid.NewGuid(),
            Description = "Severity mapping test",
            Severity = nis2Severity,
            DetectedAtUtc = _baseTime,
            IsSignificant = true,
            AffectedServices = ["ServiceA"],
            InitialAssessment = "Test assessment"
        };

        // Act
        await sut.ReportIncidentAsync(incident);

        // Assert
        await breachService.Received(1).RecordBreachAsync(
            Arg.Any<string>(),
            Arg.Is(expectedBreachSeverity),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
