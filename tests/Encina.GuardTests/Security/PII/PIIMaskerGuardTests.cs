using Encina.Security.PII;
using Encina.Security.PII.Abstractions;

namespace Encina.GuardTests.Security.PII;

/// <summary>
/// Guard clause tests for <see cref="PIIMasker"/>.
/// Verifies null argument validation on constructor and public methods.
/// </summary>
public sealed class PIIMaskerGuardTests
{
    private static PIIMasker CreateSut() =>
        new(Options.Create(new PIIOptions()), Substitute.For<ILogger<PIIMasker>>(), new ServiceCollection().BuildServiceProvider());

    #region Constructor Guards

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PIIMasker>>();
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        var act = () => new PIIMasker(null!, logger, sp);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new PIIOptions());
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        var act = () => new PIIMasker(options, null!, sp);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new PIIOptions());
        var logger = Substitute.For<ILogger<PIIMasker>>();

        // Act & Assert
        var act = () => new PIIMasker(options, logger, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    #endregion

    #region Mask(string, string pattern) Guards

    [Fact]
    public void Mask_NullPattern_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Mask("test-value", (string)null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("pattern");
    }

    #endregion

    #region MaskObject<T> Guards

    [Fact]
    public void MaskObject_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.MaskObject<TestDto>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("obj");
    }

    #endregion

    #region MaskForLog Guards

    [Fact]
    public void MaskForLog_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.MaskForLog<TestDto>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("obj");
    }

    #endregion

    #region MaskForAudit Guards

    [Fact]
    public void MaskForAudit_Generic_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.MaskForAudit<TestDto>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public void MaskForAudit_Object_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.MaskForAudit((object)null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region Empty Input Guards

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Mask_ByType_NullOrEmptyValue_ReturnsTheInputUnchanged(string? value)
    {
        var sut = CreateSut();

        sut.Mask(value!, PIIType.Email).ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Mask_ByPattern_NullOrEmptyValue_ReturnsTheInputUnchanged(string? value)
    {
        var sut = CreateSut();

        sut.Mask(value!, "x").ShouldBe(value);
    }

    [Fact]
    public void Mask_ByPattern_MasksOnlyTheMatchingParts()
    {
        var sut = CreateSut();

        sut.Mask("abc123", @"\d+").ShouldBe("abc***");
    }

    [Fact]
    public void Mask_ByPattern_InvalidPattern_MasksTheWholeValueInsteadOfReturningIt()
    {
        var sut = CreateSut();

        sut.Mask("abc123", "[").ShouldBe("******");
    }

    [Fact]
    public void Mask_ByType_NonEmptyValue_IsNotReturnedInClear()
    {
        var sut = CreateSut();

        sut.Mask("john@example.com", PIIType.Email).ShouldNotBe("john@example.com");
    }

    #endregion

    #region Test Types

    private sealed class TestDto
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
