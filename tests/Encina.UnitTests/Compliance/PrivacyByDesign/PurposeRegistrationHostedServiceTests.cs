#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="PurposeRegistrationHostedService"/>.
/// </summary>
public class PurposeRegistrationHostedServiceTests
{
    private readonly IPurposeRegistry _registry = Substitute.For<IPurposeRegistry>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly ILogger<PurposeRegistrationHostedService> _logger = NullLogger<PurposeRegistrationHostedService>.Instance;

    #region StartAsync — No Purpose Builders

    [Fact]
    public async Task StartAsync_NoPurposeBuilders_ShouldNotCallRegistry()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        var service = CreateService(options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        await _registry.DidNotReceive()
            .RegisterPurposeAsync(Arg.Any<PurposeDefinition>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region StartAsync — Single Purpose Builder

    [Fact]
    public async Task StartAsync_OnePurposeBuilder_ShouldRegisterOnePurpose()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        options.AddPurpose("Order Processing", p =>
        {
            p.Description = "Test";
            p.LegalBasis = "Contract";
            p.AllowedFields.AddRange(["ProductId", "Quantity"]);
        });

        _registry.RegisterPurposeAsync(Arg.Any<PurposeDefinition>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));

        var service = CreateService(options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        await _registry.Received(1)
            .RegisterPurposeAsync(Arg.Any<PurposeDefinition>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region StartAsync — Multiple Purpose Builders

    [Fact]
    public async Task StartAsync_MultiplePurposeBuilders_ShouldRegisterAll()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        options.AddPurpose("Order Processing", p =>
        {
            p.Description = "Orders";
            p.LegalBasis = "Contract";
            p.AllowedFields.AddRange(["ProductId", "Quantity"]);
        });
        options.AddPurpose("Marketing", p =>
        {
            p.Description = "Marketing";
            p.LegalBasis = "Consent";
            p.AllowedFields.AddRange(["Email"]);
        });
        options.AddPurpose("Analytics", p =>
        {
            p.Description = "Analytics";
            p.LegalBasis = "Legitimate Interest";
            p.AllowedFields.AddRange(["SessionId"]);
        });

        _registry.RegisterPurposeAsync(Arg.Any<PurposeDefinition>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));

        var service = CreateService(options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        await _registry.Received(3)
            .RegisterPurposeAsync(Arg.Any<PurposeDefinition>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region StartAsync — Registry Error for One Purpose

    [Fact]
    public async Task StartAsync_RegistryReturnsErrorForOne_ShouldContinueWithOthers()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        options.AddPurpose("Failing Purpose", p =>
        {
            p.Description = "Will fail";
            p.LegalBasis = "Contract";
        });
        options.AddPurpose("Succeeding Purpose", p =>
        {
            p.Description = "Will succeed";
            p.LegalBasis = "Consent";
        });

        var callCount = 0;
        _registry.RegisterPurposeAsync(Arg.Any<PurposeDefinition>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return ValueTask.FromResult(Left<EncinaError, Unit>(
                        PrivacyByDesignErrors.DuplicatePurpose("Failing Purpose")));
                }

                return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
            });

        var service = CreateService(options);

        // Act
        var act = () => service.StartAsync(CancellationToken.None);

        // Assert — should not throw, should call RegisterPurposeAsync for both.
        await Should.NotThrowAsync(act);
        await _registry.Received(2)
            .RegisterPurposeAsync(Arg.Any<PurposeDefinition>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region StartAsync — Correct PurposeDefinition Values

    [Fact]
    public async Task StartAsync_ShouldCallRegistryWithCorrectPurposeDefinitionValues()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        options.AddPurpose("Order Processing", p =>
        {
            p.Description = "Fulfill orders";
            p.LegalBasis = "Contract";
            p.AllowedFields.AddRange(["ProductId", "Quantity"]);
        });

        PurposeDefinition? capturedPurpose = null;
        _registry.RegisterPurposeAsync(Arg.Any<PurposeDefinition>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedPurpose = callInfo.Arg<PurposeDefinition>();
                return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
            });

        var service = CreateService(options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        capturedPurpose.ShouldNotBeNull();
        capturedPurpose!.Name.ShouldBe("Order Processing");
        capturedPurpose.Description.ShouldBe("Fulfill orders");
        capturedPurpose.LegalBasis.ShouldBe("Contract");
        capturedPurpose.AllowedFields.ShouldBe(["ProductId", "Quantity"]);
        capturedPurpose.PurposeId.ShouldNotBeNullOrWhiteSpace();
        capturedPurpose.CreatedAtUtc.ShouldBe(_timeProvider.GetUtcNow());
    }

    #endregion

    #region StopAsync

    [Fact]
    public async Task StopAsync_ShouldReturnCompletedTask()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        var service = CreateService(options);

        // Act
        var act = () => service.StopAsync(CancellationToken.None);

        // Assert
        await Should.NotThrowAsync(act);
    }

    #endregion

    #region Helpers

    private PurposeRegistrationHostedService CreateService(PrivacyByDesignOptions options) =>
        new(_registry, Options.Create(options), _timeProvider, _logger);

    #endregion
}
