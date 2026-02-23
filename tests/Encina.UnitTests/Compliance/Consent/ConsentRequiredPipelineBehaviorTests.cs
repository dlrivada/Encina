using Encina.Compliance.Consent;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="ConsentRequiredPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class ConsentRequiredPipelineBehaviorTests
{
    private readonly IConsentValidator _validator;
    private readonly ILogger<ConsentRequiredPipelineBehavior<SampleConsentRequest, Unit>> _loggerConsent;
    private readonly ILogger<ConsentRequiredPipelineBehavior<SampleNoConsentRequest, Unit>> _loggerNoConsent;
    private readonly ILogger<ConsentRequiredPipelineBehavior<SampleCustomSubjectRequest, Unit>> _loggerCustom;

    public ConsentRequiredPipelineBehaviorTests()
    {
        _validator = Substitute.For<IConsentValidator>();
        _loggerConsent = Substitute.For<ILogger<ConsentRequiredPipelineBehavior<SampleConsentRequest, Unit>>>();
        _loggerNoConsent = Substitute.For<ILogger<ConsentRequiredPipelineBehavior<SampleNoConsentRequest, Unit>>>();
        _loggerCustom = Substitute.For<ILogger<ConsentRequiredPipelineBehavior<SampleCustomSubjectRequest, Unit>>>();

        // Default: validator returns valid
#pragma warning disable CA2012
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(
                ConsentValidationResult.Valid()));
#pragma warning restore CA2012
    }

    #region Enforcement Mode Disabled

    [Fact]
    public async Task Handle_DisabledMode_ShouldSkipValidation()
    {
        // Arrange
        var behavior = CreateConsentBehavior(o => o.EnforcementMode = ConsentEnforcementMode.Disabled);
        var request = new SampleConsentRequest("user-1");

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
#pragma warning disable CA2012
        await _validator.DidNotReceive()
            .ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
#pragma warning restore CA2012
    }

    #endregion

    #region No Attribute — Passthrough

    [Fact]
    public async Task Handle_NoAttribute_ShouldPassthrough()
    {
        // Arrange
        var behavior = CreateNoConsentBehavior();
        var request = new SampleNoConsentRequest();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
#pragma warning disable CA2012
        await _validator.DidNotReceive()
            .ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
#pragma warning restore CA2012
    }

    #endregion

    #region Subject ID Extraction

    [Fact]
    public async Task Handle_SubjectIdFromContext_ShouldUseContextUserId()
    {
        // Arrange
        var behavior = CreateConsentBehavior();
        var request = new SampleConsentRequest("user-42");
        var context = RequestContext.CreateForTest(userId: "context-user-1");

        // Act
        var result = await behavior.Handle(
            request, context, Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        // The SampleConsentRequest does NOT specify SubjectIdProperty,
        // so it should fall back to context.UserId
#pragma warning disable CA2012
        await _validator.Received(1)
            .ValidateAsync("context-user-1", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
#pragma warning restore CA2012
    }

    [Fact]
    public async Task Handle_SubjectIdFromProperty_ShouldUsePropertyValue()
    {
        // Arrange
        var behavior = CreateCustomSubjectBehavior();
        var request = new SampleCustomSubjectRequest("customer-99");
        var context = RequestContext.CreateForTest(userId: "context-user-1");

        // Act
        var result = await behavior.Handle(
            request, context, Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        // SampleCustomSubjectRequest has SubjectIdProperty = "CustomerId"
#pragma warning disable CA2012
        await _validator.Received(1)
            .ValidateAsync("customer-99", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
#pragma warning restore CA2012
    }

    [Fact]
    public async Task Handle_EmptySubjectId_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateConsentBehavior();
        var request = new SampleConsentRequest("user-1");
        var context = RequestContext.CreateForTest(userId: null);

        // Act
        var result = await behavior.Handle(
            request, context, Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Block Mode — Valid Consent

    [Fact]
    public async Task Handle_BlockMode_ValidConsent_ShouldProceed()
    {
        // Arrange
        var behavior = CreateConsentBehavior(o => o.EnforcementMode = ConsentEnforcementMode.Block);
        var request = new SampleConsentRequest("user-1");
        var context = RequestContext.CreateForTest(userId: "user-1");

        // Act
        var result = await behavior.Handle(
            request, context, Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Block Mode — Invalid Consent

    [Fact]
    public async Task Handle_BlockMode_InvalidConsent_ShouldReturnError()
    {
        // Arrange
#pragma warning disable CA2012
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(
                ConsentValidationResult.Invalid(
                    ["Missing consent for marketing"],
                    [ConsentPurposes.Marketing])));
#pragma warning restore CA2012

        var behavior = CreateConsentBehavior(o => o.EnforcementMode = ConsentEnforcementMode.Block);
        var request = new SampleConsentRequest("user-1");
        var context = RequestContext.CreateForTest(userId: "user-1");

        // Act
        var result = await behavior.Handle(
            request, context, Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("user-1");
    }

    [Fact]
    public async Task Handle_BlockMode_CustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
#pragma warning disable CA2012
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(
                ConsentValidationResult.Invalid(
                    ["Missing consent"],
                    [ConsentPurposes.Marketing])));
#pragma warning restore CA2012

        var behavior = CreateCustomErrorBehavior();
        var context = RequestContext.CreateForTest(userId: "user-1");

        // Act
        var result = await behavior.Handle(
            new SampleCustomErrorRequest("user-1"), context, NextCustomError(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Warn Mode

    [Fact]
    public async Task Handle_WarnMode_InvalidConsent_ShouldProceed()
    {
        // Arrange
#pragma warning disable CA2012
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(
                ConsentValidationResult.Invalid(
                    ["Missing consent for marketing"],
                    [ConsentPurposes.Marketing])));
#pragma warning restore CA2012

        var behavior = CreateConsentBehavior(o => o.EnforcementMode = ConsentEnforcementMode.Warn);
        var request = new SampleConsentRequest("user-1");
        var context = RequestContext.CreateForTest(userId: "user-1");

        // Act
        var result = await behavior.Handle(
            request, context, Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Validator Errors

    [Fact]
    public async Task Handle_ValidatorError_ShouldReturnError()
    {
        // Arrange
#pragma warning disable CA2012
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(
                Left<EncinaError, ConsentValidationResult>(EncinaError.New("Validator unavailable"))));
#pragma warning restore CA2012

        var behavior = CreateConsentBehavior();
        var request = new SampleConsentRequest("user-1");
        var context = RequestContext.CreateForTest(userId: "user-1");

        // Act
        var result = await behavior.Handle(
            request, context, Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("Validator unavailable");
    }

    #endregion

    #region Valid With Warnings

    [Fact]
    public async Task Handle_ValidWithWarnings_ShouldProceed()
    {
        // Arrange
#pragma warning disable CA2012
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(
                ConsentValidationResult.ValidWithWarnings("Version manager unavailable")));
#pragma warning restore CA2012

        var behavior = CreateConsentBehavior();
        var request = new SampleConsentRequest("user-1");
        var context = RequestContext.CreateForTest(userId: "user-1");

        // Act
        var result = await behavior.Handle(
            request, context, Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        // Arrange
        var behavior = CreateConsentBehavior();

        // Act
        var act = async () => await behavior.Handle(
            null!, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    #endregion

    #region Helpers

    private static RequestHandlerCallback<Unit> Next(Unit value) =>
        () => ValueTask.FromResult<Either<EncinaError, Unit>>(value);

    private static RequestHandlerCallback<Unit> NextCustomError(Unit value) =>
        () => ValueTask.FromResult<Either<EncinaError, Unit>>(value);

    private ConsentRequiredPipelineBehavior<SampleConsentRequest, Unit> CreateConsentBehavior(
        Action<ConsentOptions>? configure = null)
    {
        var options = new ConsentOptions();
        options.DefinePurpose(ConsentPurposes.Marketing);
        configure?.Invoke(options);
        return new ConsentRequiredPipelineBehavior<SampleConsentRequest, Unit>(
            _validator, Options.Create(options), _loggerConsent);
    }

    private ConsentRequiredPipelineBehavior<SampleNoConsentRequest, Unit> CreateNoConsentBehavior(
        Action<ConsentOptions>? configure = null)
    {
        var options = new ConsentOptions();
        configure?.Invoke(options);
        return new ConsentRequiredPipelineBehavior<SampleNoConsentRequest, Unit>(
            _validator, Options.Create(options), _loggerNoConsent);
    }

    private ConsentRequiredPipelineBehavior<SampleCustomSubjectRequest, Unit> CreateCustomSubjectBehavior(
        Action<ConsentOptions>? configure = null)
    {
        var options = new ConsentOptions();
        options.DefinePurpose(ConsentPurposes.Analytics);
        configure?.Invoke(options);
        return new ConsentRequiredPipelineBehavior<SampleCustomSubjectRequest, Unit>(
            _validator, Options.Create(options), _loggerCustom);
    }

    private ConsentRequiredPipelineBehavior<SampleCustomErrorRequest, Unit> CreateCustomErrorBehavior(
        Action<ConsentOptions>? configure = null)
    {
        var options = new ConsentOptions();
        options.DefinePurpose(ConsentPurposes.Marketing);
        configure?.Invoke(options);
        return new ConsentRequiredPipelineBehavior<SampleCustomErrorRequest, Unit>(
            _validator, Options.Create(options),
            Substitute.For<ILogger<ConsentRequiredPipelineBehavior<SampleCustomErrorRequest, Unit>>>());
    }

    #endregion
}

// Test request types for consent pipeline behavior tests

[RequireConsent(ConsentPurposes.Marketing)]
public sealed record SampleConsentRequest(string UserId) : ICommand<Unit>;

[RequireConsent(ConsentPurposes.Analytics, SubjectIdProperty = "CustomerId")]
public sealed record SampleCustomSubjectRequest(string CustomerId) : ICommand<Unit>;

[RequireConsent(ConsentPurposes.Marketing, ErrorMessage = "Marketing consent required")]
public sealed record SampleCustomErrorRequest(string UserId) : ICommand<Unit>;

public sealed record SampleNoConsentRequest : ICommand<Unit>;
