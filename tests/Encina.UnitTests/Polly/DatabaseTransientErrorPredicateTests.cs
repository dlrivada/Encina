using System.Data.Common;
using System.Net.Sockets;

using Encina.Database;
using Encina.Polly.Predicates;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Unit tests for <see cref="DatabaseTransientErrorPredicate"/>.
/// </summary>
public sealed class DatabaseTransientErrorPredicateTests
{
    #region Constructor

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DatabaseTransientErrorPredicate(null!));
    }

    #endregion

    #region IsTransient — Null

    [Fact]
    public void IsTransient_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        var predicate = CreatePredicate();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => predicate.IsTransient(null!));
    }

    #endregion

    #region IsTransient — Database Exceptions

    [Fact]
    public void IsTransient_DbException_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate();
        var ex = new TestDbException("test");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_GenericException_ReturnsFalse()
    {
        // Arrange
        var predicate = CreatePredicate();
        var ex = new InvalidOperationException("not transient");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTransient_ArgumentException_ReturnsFalse()
    {
        // Arrange
        var predicate = CreatePredicate();
        var ex = new ArgumentException("not transient");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsTransient — Timeout Exceptions

    [Fact]
    public void IsTransient_TimeoutException_WithIncludeTimeouts_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate(includeTimeouts: true);
        var ex = new TimeoutException("timed out");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_TimeoutException_WithoutIncludeTimeouts_ReturnsFalse()
    {
        // Arrange
        var predicate = CreatePredicate(includeTimeouts: false);
        var ex = new TimeoutException("timed out");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTransient_TaskCanceledException_WithIncludeTimeouts_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate(includeTimeouts: true);
        var ex = new TaskCanceledException("cancelled");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_OperationCanceledException_WithIncludeTimeouts_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate(includeTimeouts: true);
        var ex = new OperationCanceledException("cancelled");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region IsTransient — Connection Failures

    [Fact]
    public void IsTransient_SocketException_WithIncludeConnectionFailures_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate(includeConnectionFailures: true);
        var ex = new SocketException();

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_SocketException_WithoutIncludeConnectionFailures_ReturnsFalse()
    {
        // Arrange
        var predicate = CreatePredicate(includeConnectionFailures: false);
        var ex = new SocketException();

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTransient_IOException_WithIncludeConnectionFailures_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate(includeConnectionFailures: true);
        var ex = new IOException("connection reset");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_HttpRequestException_WithIncludeConnectionFailures_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate(includeConnectionFailures: true);
        var ex = new HttpRequestException("connection failed");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region IsTransient — Inner Exceptions

    [Fact]
    public void IsTransient_WrappedDbException_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate();
        var inner = new TestDbException("inner");
        var ex = new InvalidOperationException("wrapper", inner);

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_DeeplyNestedTransient_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate(includeConnectionFailures: true);
        var socket = new SocketException();
        var io = new IOException("io", socket);
        var wrapper = new InvalidOperationException("wrapper", io);

        // Act
        var result = predicate.IsTransient(wrapper);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_WrappedNonTransient_ReturnsFalse()
    {
        // Arrange
        var predicate = CreatePredicate();
        var inner = new ArgumentException("bad arg");
        var ex = new InvalidOperationException("wrapper", inner);

        // Act
        var result = predicate.IsTransient(ex);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsTransient — Type Hierarchy

    [Fact]
    public void IsTransient_DerivedFromDbException_ReturnsTrue()
    {
        // Arrange
        var predicate = CreatePredicate();
        var ex = new DerivedDbException("derived");

        // Act
        var result = predicate.IsTransient(ex);

        // Assert — Should match via type hierarchy walk
        result.ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private static DatabaseTransientErrorPredicate CreatePredicate(
        bool includeTimeouts = true,
        bool includeConnectionFailures = true)
    {
        var options = new DatabaseCircuitBreakerOptions
        {
            IncludeTimeouts = includeTimeouts,
            IncludeConnectionFailures = includeConnectionFailures
        };
        return new DatabaseTransientErrorPredicate(options);
    }

    /// <summary>
    /// Concrete DbException for testing since DbException is abstract.
    /// </summary>
    private sealed class TestDbException : DbException
    {
        public TestDbException(string message) : base(message) { }
    }

    /// <summary>
    /// Derived from TestDbException to verify type hierarchy walking.
    /// </summary>
    private sealed class DerivedDbException : DbException
    {
        public DerivedDbException(string message) : base(message) { }
    }

    #endregion
}
