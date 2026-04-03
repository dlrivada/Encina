using Encina.Marten;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.ContractTests.Marten.Core;

/// <summary>
/// Behavioral contract tests for <see cref="EventPublishingPipelineBehavior{TRequest, TResponse}"/>
/// verifying the pipeline correctly propagates results and publishes domain events.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class EventPublishingPipelineBehaviorContractTests
{
    #region Structural Contracts

    [Fact]
    public void EventPublishingPipelineBehavior_ImplementsIPipelineBehavior()
    {
        typeof(EventPublishingPipelineBehavior<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void EventPublishingPipelineBehavior_IsSealed()
    {
        typeof(EventPublishingPipelineBehavior<,>).IsSealed.ShouldBeTrue();
    }

    #endregion

    #region Constructor Guards

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNull()
        => Shouldly.Should.Throw<ArgumentNullException>(() =>
            new EventPublishingPipelineBehavior<TestCommand, string>(
                null!,
                Substitute.For<IEncina>(),
                NullLogger<EventPublishingPipelineBehavior<TestCommand, string>>.Instance,
                Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void Constructor_NullEncina_ThrowsArgumentNull()
        => Shouldly.Should.Throw<ArgumentNullException>(() =>
            new EventPublishingPipelineBehavior<TestCommand, string>(
                Substitute.For<IDocumentSession>(),
                null!,
                NullLogger<EventPublishingPipelineBehavior<TestCommand, string>>.Instance,
                Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNull()
        => Shouldly.Should.Throw<ArgumentNullException>(() =>
            new EventPublishingPipelineBehavior<TestCommand, string>(
                Substitute.For<IDocumentSession>(),
                Substitute.For<IEncina>(),
                null!,
                Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNull()
        => Shouldly.Should.Throw<ArgumentNullException>(() =>
            new EventPublishingPipelineBehavior<TestCommand, string>(
                Substitute.For<IDocumentSession>(),
                Substitute.For<IEncina>(),
                NullLogger<EventPublishingPipelineBehavior<TestCommand, string>>.Instance,
                null!));

    #endregion

    #region Handle Contract - Left Propagation

    [Fact]
    public async Task Handle_WhenNextStepReturnsLeft_PropagatesLeft()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var encina = Substitute.For<IEncina>();
        var options = new EncinaMartenOptions { AutoPublishDomainEvents = true };

        var behavior = new EventPublishingPipelineBehavior<TestCommand, string>(
            session,
            encina,
            NullLogger<EventPublishingPipelineBehavior<TestCommand, string>>.Instance,
            Microsoft.Extensions.Options.Options.Create(options));

        var expectedError = EncinaErrors.Create("TEST_ERROR", "test failure");
        RequestHandlerCallback<string> callback = () =>
            new ValueTask<Either<EncinaError, string>>(
                LanguageExt.Prelude.Left<EncinaError, string>(expectedError));

        // Act
        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            callback,
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handle Contract - AutoPublish Disabled

    [Fact]
    public async Task Handle_WhenAutoPublishDisabled_ReturnsRightFromCallback()
    {
        // Arrange
        var session = Substitute.For<IDocumentSession>();
        var encina = Substitute.For<IEncina>();
        var options = new EncinaMartenOptions { AutoPublishDomainEvents = false };

        var behavior = new EventPublishingPipelineBehavior<TestCommand, string>(
            session,
            encina,
            NullLogger<EventPublishingPipelineBehavior<TestCommand, string>>.Instance,
            Microsoft.Extensions.Options.Options.Create(options));

        RequestHandlerCallback<string> callback = () =>
            new ValueTask<Either<EncinaError, string>>(
                LanguageExt.Prelude.Right<EncinaError, string>("success"));

        // Act
        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            callback,
            CancellationToken.None);

        // Assert: returns the callback result directly when auto-publish is disabled
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Test Types

    public sealed record TestCommand : ICommand<string>;

    #endregion
}
