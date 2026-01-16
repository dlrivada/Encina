using Encina.Messaging.ContentRouter;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.ContentRouter;

/// <summary>
/// Unit tests for <see cref="RouteDefinition{TMessage, TResult}"/>.
/// </summary>
public sealed class RouteDefinitionTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsAllProperties()
    {
        // Arrange
        const string name = "TestRoute";
        Func<TestMessage, bool> condition = m => m.Value > 5;
        Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>> handler =
            (m, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"));
        const int priority = 10;
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var route = new RouteDefinition<TestMessage, string>(
            name, condition, handler, priority, isDefault: true, metadata);

        // Assert
        route.Name.ShouldBe(name);
        route.Condition.ShouldBe(condition);
        route.Handler.ShouldBe(handler);
        route.Priority.ShouldBe(priority);
        route.IsDefault.ShouldBeTrue();
        route.Metadata.ShouldNotBeNull();
        route.Metadata["key"].ShouldBe("value");
    }

    [Fact]
    public void Constructor_WithDefaultPriorityAndMetadata_UsesDefaults()
    {
        // Arrange
        const string name = "TestRoute";
        Func<TestMessage, bool> condition = m => true;
        Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>> handler =
            (m, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"));

        // Act
        var route = new RouteDefinition<TestMessage, string>(name, condition, handler);

        // Assert
        route.Priority.ShouldBe(0);
        route.IsDefault.ShouldBeFalse();
        route.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        Func<TestMessage, bool> condition = m => true;
        Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>> handler =
            (m, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"));

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new RouteDefinition<TestMessage, string>(null!, condition, handler));
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        Func<TestMessage, bool> condition = m => true;
        Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>> handler =
            (m, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"));

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new RouteDefinition<TestMessage, string>(string.Empty, condition, handler));
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        Func<TestMessage, bool> condition = m => true;
        Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>> handler =
            (m, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"));

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new RouteDefinition<TestMessage, string>("  ", condition, handler));
    }

    [Fact]
    public void Constructor_WithNullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>> handler =
            (m, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RouteDefinition<TestMessage, string>("TestRoute", null!, handler));
    }

    [Fact]
    public void Constructor_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        Func<TestMessage, bool> condition = m => true;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RouteDefinition<TestMessage, string>("TestRoute", condition, null!));
    }

    [Fact]
    public void Matches_WhenConditionReturnsTrue_ReturnsTrue()
    {
        // Arrange
        var route = new RouteDefinition<TestMessage, string>(
            "TestRoute",
            m => m.Value > 5,
            (m, ct) => ValueTask.FromResult(Right<EncinaError, string>("result")));

        var message = new TestMessage { Value = 10 };

        // Act
        var result = route.Matches(message);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Matches_WhenConditionReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var route = new RouteDefinition<TestMessage, string>(
            "TestRoute",
            m => m.Value > 5,
            (m, ct) => ValueTask.FromResult(Right<EncinaError, string>("result")));

        var message = new TestMessage { Value = 3 };

        // Act
        var result = route.Matches(message);

        // Assert
        result.ShouldBeFalse();
    }

    private sealed class TestMessage
    {
        public int Value { get; set; }
    }
}
