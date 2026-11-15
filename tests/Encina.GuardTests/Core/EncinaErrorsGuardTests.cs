namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for the <see cref="ErrorCategory"/> overloads of <see cref="EncinaErrors"/>.
/// </summary>
public class EncinaErrorsGuardTests
{
    #region Create(code, category, message, exception, details)

    [Fact]
    public void Create_NullCode_ThrowsArgumentNullException()
    {
        Action act = () => _ = EncinaErrors.Create(null!, ErrorCategory.NotFound, "message");

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("code");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceCode_ThrowsArgumentException(string code)
    {
        Action act = () => _ = EncinaErrors.Create(code, ErrorCategory.NotFound, "message");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("code");
    }

    [Fact]
    public void Create_NullMessage_ThrowsArgumentNullException()
    {
        Action act = () => _ = EncinaErrors.Create("orders.not_found", ErrorCategory.NotFound, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("message");
    }

    [Fact]
    public void Create_NullExceptionAndDetails_ReturnsErrorWithCategory()
    {
        var error = EncinaErrors.Create("orders.not_found", ErrorCategory.NotFound, "message", exception: null, details: null);

        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCategory().ShouldBe(ErrorCategory.NotFound);
    }

    #endregion

    #region FromException(code, category, exception, message, details)

    [Fact]
    public void FromException_NullCode_ThrowsArgumentNullException()
    {
        Action act = () => _ = EncinaErrors.FromException(null!, ErrorCategory.Internal, new InvalidOperationException("boom"));

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("code");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromException_EmptyOrWhitespaceCode_ThrowsArgumentException(string code)
    {
        Action act = () => _ = EncinaErrors.FromException(code, ErrorCategory.Internal, new InvalidOperationException("boom"));

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("code");
    }

    [Fact]
    public void FromException_NullException_ThrowsArgumentNullException()
    {
        Action act = () => _ = EncinaErrors.FromException("orders.failed", ErrorCategory.Internal, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("exception");
    }

    [Fact]
    public void FromException_NullMessageAndDetails_ReturnsErrorWithCategory()
    {
        var error = EncinaErrors.FromException(
            "orders.failed", ErrorCategory.Conflict, new InvalidOperationException("boom"), message: null, details: null);

        error.GetCategory().ShouldBe(ErrorCategory.Conflict);
    }

    #endregion
}
