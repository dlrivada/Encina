using Encina.Cdc.MongoDb;
using MongoDB.Bson;

namespace Encina.GuardTests.Cdc.MongoDb;

/// <summary>
/// Guard clause tests for <see cref="MongoCdcPosition"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class MongoCdcPositionGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when resumeToken is null.
    /// </summary>
    [Fact]
    public void Constructor_NullResumeToken_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MongoCdcPosition(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resumeToken");
    }

    #endregion

    #region FromBytes Guards

    /// <summary>
    /// Verifies that FromBytes throws ArgumentNullException when bytes is null.
    /// </summary>
    [Fact]
    public void FromBytes_NullBytes_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => MongoCdcPosition.FromBytes(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("bytes");
    }

    #endregion
}
