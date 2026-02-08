using Encina.Database;
using Encina.Polly.Predicates;

using Shouldly;

namespace Encina.GuardTests.Database;

/// <summary>
/// Guard clause tests for <see cref="DatabaseTransientErrorPredicate"/>.
/// </summary>
public sealed class DatabaseTransientErrorPredicateGuardsTests
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new DatabaseTransientErrorPredicate(null!));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void IsTransient_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        var predicate = new DatabaseTransientErrorPredicate(new DatabaseCircuitBreakerOptions());

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => predicate.IsTransient(null!));
        ex.ParamName.ShouldBe("exception");
    }
}
