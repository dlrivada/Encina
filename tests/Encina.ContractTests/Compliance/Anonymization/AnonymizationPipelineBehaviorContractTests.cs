using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Techniques;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.ContractTests.Compliance.Anonymization;

/// <summary>
/// Behavioral contract tests for <see cref="AnonymizationPipelineBehavior{TRequest, TResponse}"/>.
/// These tests instantiate the real pipeline behavior and call Handle() to verify contracts.
/// </summary>
[Trait("Category", "Contract")]
public sealed class AnonymizationPipelineBehaviorContractTests
{
    [Fact]
    public async Task DisabledMode_SkipsAnonymization_CallsNext()
    {
        // Arrange
        var behavior = CreateBehavior(AnonymizationEnforcementMode.Disabled);
        var context = CreateContext();
        var called = false;

        RequestHandlerCallback<Unit> next = () =>
        {
            called = true;
            return new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(new PlainCommand(), context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task NoAttribute_SkipsAnonymization_CallsNext()
    {
        // Arrange — Block mode but PlainCommand/Unit have no anonymization attributes
        var behavior = CreateBehavior(AnonymizationEnforcementMode.Block);
        var context = CreateContext();
        var called = false;

        RequestHandlerCallback<Unit> next = () =>
        {
            called = true;
            return new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(new PlainCommand(), context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidRequest_CallsNext()
    {
        // Arrange — Warn mode, no attributes on response type
        var behavior = CreateBehavior(AnonymizationEnforcementMode.Warn);
        var context = CreateContext();
        var called = false;

        RequestHandlerCallback<Unit> next = () =>
        {
            called = true;
            return new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(new PlainCommand(), context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    private static AnonymizationPipelineBehavior<PlainCommand, Unit> CreateBehavior(
        AnonymizationEnforcementMode mode)
    {
        var techniques = Enumerable.Empty<IAnonymizationTechnique>();
        var pseudonymizer = Substitute.For<IPseudonymizer>();
        var tokenizer = Substitute.For<ITokenizer>();
        var keyProvider = Substitute.For<IKeyProvider>();
        var options = Options.Create(new AnonymizationOptions { EnforcementMode = mode });
        var logger = NullLogger<AnonymizationPipelineBehavior<PlainCommand, Unit>>.Instance;

        return new AnonymizationPipelineBehavior<PlainCommand, Unit>(
            techniques, pseudonymizer, tokenizer, keyProvider, options, logger);
    }

    private static IRequestContext CreateContext()
    {
        var ctx = Substitute.For<IRequestContext>();
        ctx.CorrelationId.Returns("corr-contract-1");
        return ctx;
    }

    /// <summary>
    /// A plain command with no anonymization attributes on its response type.
    /// </summary>
    public sealed record PlainCommand : IRequest<Unit>;
}
