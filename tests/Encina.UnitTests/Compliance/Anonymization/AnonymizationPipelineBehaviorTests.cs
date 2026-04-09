#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="AnonymizationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class AnonymizationPipelineBehaviorTests
{
    // ================================================================
    // Test types — plain response (no attributes)
    // ================================================================

    public sealed record PlainResponse(string Name);

    public sealed record PlainCommand : IRequest<PlainResponse>;

    // ================================================================
    // Test types — decorated response with all three attribute types
    // ================================================================

    public sealed class DecoratedResponse
    {
        [Anonymize(Technique = AnonymizationTechnique.DataMasking)]
        public string FullName { get; set; } = string.Empty;

        [Pseudonymize(KeyId = "test-key", Algorithm = PseudonymizationAlgorithm.Aes256Gcm)]
        public string Email { get; set; } = string.Empty;

        [Tokenize(Format = TokenFormat.Prefixed, Prefix = "tok")]
        public string CreditCard { get; set; } = string.Empty;
    }

    public sealed record DecoratedCommand : IRequest<DecoratedResponse>;

    // ================================================================
    // Test types — single anonymize attribute
    // ================================================================

    public sealed class AnonymizeOnlyResponse
    {
        [Anonymize(Technique = AnonymizationTechnique.DataMasking)]
        public string Name { get; set; } = string.Empty;
    }

    public sealed record AnonymizeOnlyCommand : IRequest<AnonymizeOnlyResponse>;

    // ================================================================
    // Test types — single pseudonymize attribute (no KeyId, tests fallback)
    // ================================================================

    public sealed class PseudonymizeNoKeyResponse
    {
        [Pseudonymize(Algorithm = PseudonymizationAlgorithm.HmacSha256)]
        public string Secret { get; set; } = string.Empty;
    }

    public sealed record PseudonymizeNoKeyCommand : IRequest<PseudonymizeNoKeyResponse>;

    // ================================================================
    // Test types — pseudonymize with non-string value
    // ================================================================

    public sealed class PseudonymizeNonStringResponse
    {
        [Pseudonymize(KeyId = "key")]
        public int Value { get; set; }
    }

    public sealed record PseudonymizeNonStringCommand : IRequest<PseudonymizeNonStringResponse>;

    // ================================================================
    // Test types — tokenize with non-string value
    // ================================================================

    public sealed class TokenizeNonStringResponse
    {
        [Tokenize(Format = TokenFormat.Uuid)]
        public int Code { get; set; }
    }

    public sealed record TokenizeNonStringCommand : IRequest<TokenizeNonStringResponse>;

    // ================================================================
    // Shared mocks
    // ================================================================

    private readonly IAnonymizationTechnique _dataMaskingTechnique;
    private readonly IPseudonymizer _pseudonymizer;
    private readonly ITokenizer _tokenizer;
    private readonly IKeyProvider _keyProvider;

    public AnonymizationPipelineBehaviorTests()
    {
        _dataMaskingTechnique = Substitute.For<IAnonymizationTechnique>();
        _dataMaskingTechnique.Technique.Returns(AnonymizationTechnique.DataMasking);
        _dataMaskingTechnique.CanApply(Arg.Any<Type>()).Returns(true);
        _dataMaskingTechnique
            .ApplyAsync(Arg.Any<object?>(), Arg.Any<Type>(), Arg.Any<IReadOnlyDictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, object?>("***masked***"));

        _pseudonymizer = Substitute.For<IPseudonymizer>();
        _pseudonymizer
            .PseudonymizeValueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<PseudonymizationAlgorithm>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("pseudo-value"));

        _tokenizer = Substitute.For<ITokenizer>();
        _tokenizer
            .TokenizeAsync(Arg.Any<string>(), Arg.Any<TokenizationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("tok_abc123"));

        _keyProvider = Substitute.For<IKeyProvider>();
        _keyProvider
            .GetActiveKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("active-key-1"));
    }

    // ================================================================
    // Step 1: Disabled mode
    // ================================================================

    [Fact]
    public async Task Handle_DisabledMode_CallsNextDirectly()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior(o => o.EnforcementMode = AnonymizationEnforcementMode.Disabled);
        var response = new DecoratedResponse { FullName = "John Doe", Email = "john@example.com", CreditCard = "4111111111111111" };

        // Act
        var result = await behavior.Handle(
            new DecoratedCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        var actual = (DecoratedResponse)result;
        actual.FullName.Should().Be("John Doe", "no transformation should occur in Disabled mode");
        actual.Email.Should().Be("john@example.com");
        actual.CreditCard.Should().Be("4111111111111111");
    }

    // ================================================================
    // Step 2: No attributes on response type
    // ================================================================

    [Fact]
    public async Task Handle_NoAttributes_CallsNextDirectly()
    {
        // Arrange
        var behavior = CreatePlainBehavior();
        var response = new PlainResponse("hello");

        // Act
        var result = await behavior.Handle(
            new PlainCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        ((PlainResponse)result).Name.Should().Be("hello");
    }

    // ================================================================
    // Step 3: Handler returns Left (error pass-through)
    // ================================================================

    [Fact]
    public async Task Handle_HandlerReturnsLeft_PassesThrough()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior();
        var error = EncinaErrors.Create(code: "test.error", message: "handler failed");

        // Act
        var result = await behavior.Handle(
            new DecoratedCommand(),
            RequestContext.CreateForTest(),
            NextLeft<DecoratedResponse>(error),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        ((EncinaError)result).Message.Should().Contain("handler failed");
    }

    // ================================================================
    // Step 4: Anonymize attribute applies technique
    // ================================================================

    [Fact]
    public async Task Handle_AnonymizeAttribute_AppliesTechnique()
    {
        // Arrange
        var behavior = CreateAnonymizeOnlyBehavior();
        var response = new AnonymizeOnlyResponse { Name = "John Doe" };

        // Act
        var result = await behavior.Handle(
            new AnonymizeOnlyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        ((AnonymizeOnlyResponse)result).Name.Should().Be("***masked***");
        await _dataMaskingTechnique.Received(1)
            .ApplyAsync("John Doe", typeof(string), Arg.Any<IReadOnlyDictionary<string, object>?>(), Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Step 5: Pseudonymize attribute calls pseudonymizer
    // ================================================================

    [Fact]
    public async Task Handle_PseudonymizeAttribute_CallsPseudonymizer()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior();
        var response = new DecoratedResponse { FullName = "Jane Doe", Email = "jane@example.com", CreditCard = "4111111111111111" };

        // Act
        var result = await behavior.Handle(
            new DecoratedCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        ((DecoratedResponse)result).Email.Should().Be("pseudo-value");
        await _pseudonymizer.Received(1)
            .PseudonymizeValueAsync("jane@example.com", "test-key", PseudonymizationAlgorithm.Aes256Gcm, Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Step 6: Tokenize attribute calls tokenizer
    // ================================================================

    [Fact]
    public async Task Handle_TokenizeAttribute_CallsTokenizer()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior();
        var response = new DecoratedResponse { FullName = "Jane Doe", Email = "jane@example.com", CreditCard = "4111111111111111" };

        // Act
        var result = await behavior.Handle(
            new DecoratedCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        ((DecoratedResponse)result).CreditCard.Should().Be("tok_abc123");
        await _tokenizer.Received(1)
            .TokenizeAsync("4111111111111111", Arg.Any<TokenizationOptions>(), Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Step 7: Transformation fails in Block mode
    // ================================================================

    [Fact]
    public async Task Handle_TransformationFails_BlockMode_ReturnsError()
    {
        // Arrange — technique returns Left
        var failTechnique = Substitute.For<IAnonymizationTechnique>();
        failTechnique.Technique.Returns(AnonymizationTechnique.DataMasking);
        failTechnique.CanApply(Arg.Any<Type>()).Returns(true);
        failTechnique
            .ApplyAsync(Arg.Any<object?>(), Arg.Any<Type>(), Arg.Any<IReadOnlyDictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, object?>(
                AnonymizationErrors.AnonymizationFailed("Name", "masking failed")));

        var behavior = CreateAnonymizeOnlyBehavior(
            techniques: [failTechnique],
            configure: o => o.EnforcementMode = AnonymizationEnforcementMode.Block);
        var response = new AnonymizeOnlyResponse { Name = "John Doe" };

        // Act
        var result = await behavior.Handle(
            new AnonymizeOnlyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        ((EncinaError)result).Message.Should().Contain("Anonymization failed");
    }

    // ================================================================
    // Step 8: Transformation fails in Warn mode
    // ================================================================

    [Fact]
    public async Task Handle_TransformationFails_WarnMode_ContinuesWithOriginal()
    {
        // Arrange — technique returns Left
        var failTechnique = Substitute.For<IAnonymizationTechnique>();
        failTechnique.Technique.Returns(AnonymizationTechnique.DataMasking);
        failTechnique.CanApply(Arg.Any<Type>()).Returns(true);
        failTechnique
            .ApplyAsync(Arg.Any<object?>(), Arg.Any<Type>(), Arg.Any<IReadOnlyDictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, object?>(
                AnonymizationErrors.AnonymizationFailed("Name", "masking failed")));

        var behavior = CreateAnonymizeOnlyBehavior(
            techniques: [failTechnique],
            configure: o => o.EnforcementMode = AnonymizationEnforcementMode.Warn);
        var response = new AnonymizeOnlyResponse { Name = "John Doe" };

        // Act
        var result = await behavior.Handle(
            new AnonymizeOnlyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert — should succeed but original value untouched
        result.IsRight.Should().BeTrue();
        ((AnonymizeOnlyResponse)result).Name.Should().Be("John Doe", "Warn mode should not modify the field on failure");
    }

    // ================================================================
    // Step 9: Multiple fields transformed
    // ================================================================

    [Fact]
    public async Task Handle_MultipleFields_TransformsAll()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior();
        var response = new DecoratedResponse { FullName = "Jane Doe", Email = "jane@example.com", CreditCard = "4111111111111111" };

        // Act
        var result = await behavior.Handle(
            new DecoratedCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        var actual = (DecoratedResponse)result;
        actual.FullName.Should().Be("***masked***", "Anonymize should apply DataMasking");
        actual.Email.Should().Be("pseudo-value", "Pseudonymize should apply");
        actual.CreditCard.Should().Be("tok_abc123", "Tokenize should apply");
    }

    // ================================================================
    // Step 10: Exception during transformation returns error
    // ================================================================

    [Fact]
    public async Task Handle_ExceptionDuringTransformation_ReturnsError()
    {
        // Arrange — technique throws exception
        var throwTechnique = Substitute.For<IAnonymizationTechnique>();
        throwTechnique.Technique.Returns(AnonymizationTechnique.DataMasking);
        throwTechnique.CanApply(Arg.Any<Type>()).Returns(true);
        throwTechnique
            .ApplyAsync(Arg.Any<object?>(), Arg.Any<Type>(), Arg.Any<IReadOnlyDictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("unexpected crash"));

        var behavior = CreateAnonymizeOnlyBehavior(techniques: [throwTechnique]);
        var response = new AnonymizeOnlyResponse { Name = "Jane" };

        // Act
        var result = await behavior.Handle(
            new AnonymizeOnlyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        ((EncinaError)result).Message.Should().Contain("Anonymization failed");
    }

    // ================================================================
    // Technique not registered
    // ================================================================

    [Fact]
    public async Task Handle_TechniqueNotRegistered_BlockMode_ReturnsError()
    {
        // Arrange — no techniques registered at all
        var behavior = CreateAnonymizeOnlyBehavior(
            techniques: [],
            configure: o => o.EnforcementMode = AnonymizationEnforcementMode.Block);
        var response = new AnonymizeOnlyResponse { Name = "John" };

        // Act
        var result = await behavior.Handle(
            new AnonymizeOnlyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        ((EncinaError)result).Message.Should().Contain("No implementation registered for anonymization technique");
    }

    // ================================================================
    // Technique CanApply returns false
    // ================================================================

    [Fact]
    public async Task Handle_TechniqueCannotApply_BlockMode_ReturnsError()
    {
        // Arrange
        var incompatibleTechnique = Substitute.For<IAnonymizationTechnique>();
        incompatibleTechnique.Technique.Returns(AnonymizationTechnique.DataMasking);
        incompatibleTechnique.CanApply(Arg.Any<Type>()).Returns(false);

        var behavior = CreateAnonymizeOnlyBehavior(
            techniques: [incompatibleTechnique],
            configure: o => o.EnforcementMode = AnonymizationEnforcementMode.Block);
        var response = new AnonymizeOnlyResponse { Name = "John" };

        // Act
        var result = await behavior.Handle(
            new AnonymizeOnlyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        ((EncinaError)result).Message.Should().Contain("cannot be applied to field");
    }

    // ================================================================
    // Pseudonymize with no KeyId falls back to active key
    // ================================================================

    [Fact]
    public async Task Handle_PseudonymizeNoKeyId_FallsBackToActiveKey()
    {
        // Arrange
        var behavior = CreatePseudonymizeNoKeyBehavior();
        var response = new PseudonymizeNoKeyResponse { Secret = "my-secret" };

        // Act
        var result = await behavior.Handle(
            new PseudonymizeNoKeyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        ((PseudonymizeNoKeyResponse)result).Secret.Should().Be("pseudo-value");
        await _keyProvider.Received(1).GetActiveKeyIdAsync(Arg.Any<CancellationToken>());
        await _pseudonymizer.Received(1)
            .PseudonymizeValueAsync("my-secret", "active-key-1", PseudonymizationAlgorithm.HmacSha256, Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Pseudonymize: no active key and no KeyId
    // ================================================================

    [Fact]
    public async Task Handle_PseudonymizeNoKeyId_NoActiveKey_BlockMode_ReturnsError()
    {
        // Arrange
        var noKeyProvider = Substitute.For<IKeyProvider>();
        noKeyProvider
            .GetActiveKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, string>(AnonymizationErrors.NoActiveKey()));

        var behavior = CreatePseudonymizeNoKeyBehavior(
            keyProvider: noKeyProvider,
            configure: o => o.EnforcementMode = AnonymizationEnforcementMode.Block);
        var response = new PseudonymizeNoKeyResponse { Secret = "my-secret" };

        // Act
        var result = await behavior.Handle(
            new PseudonymizeNoKeyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        // The error surfaces through the pipeline's exception handler wrapping
        ((EncinaError)result).Message.Should().NotBeNullOrEmpty();
    }

    // ================================================================
    // Pseudonymize with non-string value
    // ================================================================

    [Fact]
    public async Task Handle_PseudonymizeNonString_BlockMode_ReturnsError()
    {
        // Arrange
        var behavior = CreateBehavior<PseudonymizeNonStringCommand, PseudonymizeNonStringResponse>(
            configure: o => o.EnforcementMode = AnonymizationEnforcementMode.Block);
        var response = new PseudonymizeNonStringResponse { Value = 42 };

        // Act
        var result = await behavior.Handle(
            new PseudonymizeNonStringCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        ((EncinaError)result).Message.Should().Contain("Pseudonymization failed");
    }

    // ================================================================
    // Tokenize with non-string value
    // ================================================================

    [Fact]
    public async Task Handle_TokenizeNonString_BlockMode_ReturnsError()
    {
        // Arrange
        var behavior = CreateBehavior<TokenizeNonStringCommand, TokenizeNonStringResponse>(
            configure: o => o.EnforcementMode = AnonymizationEnforcementMode.Block);
        var response = new TokenizeNonStringResponse { Code = 999 };

        // Act
        var result = await behavior.Handle(
            new TokenizeNonStringCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        ((EncinaError)result).Message.Should().Contain("Tokenization failed");
    }

    // ================================================================
    // Null property value is skipped
    // ================================================================

    [Fact]
    public async Task Handle_NullPropertyValue_SkipsTransformation()
    {
        // Arrange
        var behavior = CreateAnonymizeOnlyBehavior();
        var response = new AnonymizeOnlyResponse { Name = null! };

        // Act
        var result = await behavior.Handle(
            new AnonymizeOnlyCommand(),
            RequestContext.CreateForTest(),
            Next(response),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        ((AnonymizeOnlyResponse)result).Name.Should().BeNull();
        await _dataMaskingTechnique.DidNotReceive()
            .ApplyAsync(Arg.Any<object?>(), Arg.Any<Type>(), Arg.Any<IReadOnlyDictionary<string, object>?>(), Arg.Any<CancellationToken>());
    }

    // ================================================================
    // Null request throws ArgumentNullException
    // ================================================================

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateDecoratedBehavior();
        var response = new DecoratedResponse();

        // Act
        var act = async () => await behavior.Handle(
            null!, RequestContext.CreateForTest(), Next(response), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    // ================================================================
    // Helpers
    // ================================================================

    private static RequestHandlerCallback<T> Next<T>(T value) =>
        () => ValueTask.FromResult<Either<EncinaError, T>>(value);

    private static RequestHandlerCallback<T> NextLeft<T>(EncinaError error) =>
        () => ValueTask.FromResult<Either<EncinaError, T>>(error);

    private AnonymizationPipelineBehavior<DecoratedCommand, DecoratedResponse> CreateDecoratedBehavior(
        Action<AnonymizationOptions>? configure = null)
    {
        var options = new AnonymizationOptions();
        configure?.Invoke(options);
        return new AnonymizationPipelineBehavior<DecoratedCommand, DecoratedResponse>(
            [_dataMaskingTechnique],
            _pseudonymizer,
            _tokenizer,
            _keyProvider,
            Options.Create(options),
            Substitute.For<ILogger<AnonymizationPipelineBehavior<DecoratedCommand, DecoratedResponse>>>());
    }

    private AnonymizationPipelineBehavior<PlainCommand, PlainResponse> CreatePlainBehavior(
        Action<AnonymizationOptions>? configure = null)
    {
        var options = new AnonymizationOptions();
        configure?.Invoke(options);
        return new AnonymizationPipelineBehavior<PlainCommand, PlainResponse>(
            [_dataMaskingTechnique],
            _pseudonymizer,
            _tokenizer,
            _keyProvider,
            Options.Create(options),
            Substitute.For<ILogger<AnonymizationPipelineBehavior<PlainCommand, PlainResponse>>>());
    }

    private AnonymizationPipelineBehavior<AnonymizeOnlyCommand, AnonymizeOnlyResponse> CreateAnonymizeOnlyBehavior(
        IAnonymizationTechnique[]? techniques = null,
        Action<AnonymizationOptions>? configure = null)
    {
        var options = new AnonymizationOptions();
        configure?.Invoke(options);
        return new AnonymizationPipelineBehavior<AnonymizeOnlyCommand, AnonymizeOnlyResponse>(
            techniques ?? [_dataMaskingTechnique],
            _pseudonymizer,
            _tokenizer,
            _keyProvider,
            Options.Create(options),
            Substitute.For<ILogger<AnonymizationPipelineBehavior<AnonymizeOnlyCommand, AnonymizeOnlyResponse>>>());
    }

    private AnonymizationPipelineBehavior<PseudonymizeNoKeyCommand, PseudonymizeNoKeyResponse> CreatePseudonymizeNoKeyBehavior(
        IKeyProvider? keyProvider = null,
        Action<AnonymizationOptions>? configure = null)
    {
        var options = new AnonymizationOptions();
        configure?.Invoke(options);
        return new AnonymizationPipelineBehavior<PseudonymizeNoKeyCommand, PseudonymizeNoKeyResponse>(
            [_dataMaskingTechnique],
            _pseudonymizer,
            _tokenizer,
            keyProvider ?? _keyProvider,
            Options.Create(options),
            Substitute.For<ILogger<AnonymizationPipelineBehavior<PseudonymizeNoKeyCommand, PseudonymizeNoKeyResponse>>>());
    }

    private AnonymizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        IAnonymizationTechnique[]? techniques = null,
        IKeyProvider? keyProvider = null,
        Action<AnonymizationOptions>? configure = null)
        where TRequest : IRequest<TResponse>
    {
        var options = new AnonymizationOptions();
        configure?.Invoke(options);
        return new AnonymizationPipelineBehavior<TRequest, TResponse>(
            techniques ?? [_dataMaskingTechnique],
            _pseudonymizer,
            _tokenizer,
            keyProvider ?? _keyProvider,
            Options.Create(options),
            Substitute.For<ILogger<AnonymizationPipelineBehavior<TRequest, TResponse>>>());
    }
}
