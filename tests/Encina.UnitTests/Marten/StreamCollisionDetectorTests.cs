using Encina.Marten;
using Shouldly;

namespace Encina.UnitTests.Marten;

public class StreamCollisionDetectorTests
{
    [Fact]
    public void IsStreamCollision_StreamIdCollisionTypeName_ReturnsTrue() =>
        StreamCollisionDetector.IsStreamCollision(new StreamIdCollisionException("x")).ShouldBeTrue();

    [Fact]
    public void IsStreamCollision_ExistingStreamTypeName_ReturnsTrue() =>
        StreamCollisionDetector.IsStreamCollision(new ExistingStreamFailure("x")).ShouldBeTrue();

    [Fact]
    public void IsStreamCollision_DuplicateStreamTypeName_ReturnsTrue() =>
        StreamCollisionDetector.IsStreamCollision(new DuplicateStreamException("x")).ShouldBeTrue();

    [Fact]
    public void IsStreamCollision_PostgresExceptionWithStreamConstraint_ReturnsTrue() =>
        StreamCollisionDetector.IsStreamCollision(
            new PostgresException("23505: violates unique constraint \"pkey_mt_streams_id\"")).ShouldBeTrue();

    [Fact]
    public void IsStreamCollision_PostgresExceptionAtDepthOne_ReturnsTrue() =>
        StreamCollisionDetector.IsStreamCollision(
            new WrapperException(new PostgresException("pkey_mt_streams_id"))).ShouldBeTrue();

    [Fact]
    public void IsStreamCollision_PostgresExceptionAtDepthTwo_ReturnsTrue() =>
        StreamCollisionDetector.IsStreamCollision(
            new WrapperException(new WrapperException(new PostgresException("PKEY_MT_STREAMS_ID")))).ShouldBeTrue();

    [Fact]
    public void IsStreamCollision_PostgresExceptionWithOtherConstraint_ReturnsFalse() =>
        StreamCollisionDetector.IsStreamCollision(
            new WrapperException(new PostgresException("other_key"))).ShouldBeFalse();

    [Fact]
    public void IsStreamCollision_NonPostgresTypeMentioningConstraint_ReturnsFalse() =>
        StreamCollisionDetector.IsStreamCollision(new InvalidOperationException("pkey_mt_streams_id")).ShouldBeFalse();

    [Fact]
    public void IsStreamCollision_UnrelatedExceptionWithoutInner_ReturnsFalse() =>
        StreamCollisionDetector.IsStreamCollision(new InvalidOperationException("boom")).ShouldBeFalse();

    private sealed class StreamIdCollisionException(string message) : Exception(message);

    private sealed class ExistingStreamFailure(string message) : Exception(message);

    private sealed class DuplicateStreamException(string message) : Exception(message);

    private sealed class WrapperException(Exception inner) : Exception("wrapper", inner);

    private sealed class PostgresException(string message) : Exception(message);
}
