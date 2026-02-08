using System.Text.Json;

using Encina.DomainModeling.Pagination;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.DomainModeling.Pagination;

/// <summary>
/// Guard tests for Cursor Pagination types.
/// </summary>
public sealed class CursorPaginationGuardTests
{
    #region CursorPaginationOptions Guards

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CursorPaginationOptions_WithSize_InvalidSize_ThrowsArgumentOutOfRangeException(int invalidSize)
    {
        // Arrange
        var options = CursorPaginationOptions.Default;

        // Act
        var act = () => options.WithSize(invalidSize);

        // Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("pageSize");
    }

    [Fact]
    public void CursorPaginationOptions_WithSize_ExceedsMaxPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = CursorPaginationOptions.Default;

        // Act
        var act = () => options.WithSize(CursorPaginationOptions.MaxPageSize + 1);

        // Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("pageSize");
    }

    [Fact]
    public void CursorPaginationOptions_ValidPageSize_DoesNotThrow()
    {
        // Arrange
        var options = CursorPaginationOptions.Default;

        // Act & Assert - Valid sizes should not throw
        Should.NotThrow(() => options.WithSize(1));
        Should.NotThrow(() => options.WithSize(50));
        Should.NotThrow(() => options.WithSize(CursorPaginationOptions.MaxPageSize));
    }

    #endregion

    #region CursorPaginatedResult Guards

    [Fact]
    public void CursorPaginatedResult_Map_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var result = new CursorPaginatedResult<int>
        {
            Items = [1, 2, 3],
            HasNextPage = false,
            HasPreviousPage = false
        };

        // Act
        var act = () => result.Map<string>(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("selector");
    }

    #endregion

    #region Base64JsonCursorEncoder Guards

    [Fact]
    public void Base64JsonCursorEncoder_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Base64JsonCursorEncoder(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("jsonOptions");
    }

    [Fact]
    public void Base64JsonCursorEncoder_Decode_InvalidBase64_ThrowsCursorEncodingException()
    {
        // Arrange
        var encoder = new Base64JsonCursorEncoder();
        var invalidCursor = "not-valid-base64!!!";

        // Act
        Action act = () => _ = encoder.Decode<string>(invalidCursor);

        // Assert
        Should.Throw<CursorEncodingException>(act);
    }

    [Fact]
    public void Base64JsonCursorEncoder_Decode_InvalidJson_ThrowsCursorEncodingException()
    {
        // Arrange
        var encoder = new Base64JsonCursorEncoder();
        var invalidJson = Convert.ToBase64String("not-valid-json"u8.ToArray());

        // Act
        Action act = () => _ = encoder.Decode<int>(invalidJson);

        // Assert
        Should.Throw<CursorEncodingException>(act);
    }

    #endregion

    #region CursorEncodingException Guards

    [Fact]
    public void CursorEncodingException_InvalidFormat_NullCursor_DoesNotThrow()
    {
        // Act - Factory methods should handle null gracefully
        var ex = CursorEncodingException.InvalidFormat(null!);

        // Assert
        ex.ShouldNotBeNull();
        ex.Message.ShouldContain("invalid format");
    }

    [Fact]
    public void CursorEncodingException_DeserializationFailed_NullCursor_DoesNotThrow()
    {
        // Act - Factory methods should handle null gracefully
        var ex = CursorEncodingException.DeserializationFailed<int>(null!);

        // Assert
        ex.ShouldNotBeNull();
        ex.Message.ShouldContain("could not be deserialized");
    }

    #endregion

    #region ServiceCollection Extension Guards

    [Fact]
    public void AddCursorPagination_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddCursorPagination();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddCursorPagination_WithNullJsonOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        JsonSerializerOptions? jsonOptions = null;

        // Act
        var act = () => services.AddCursorPagination(jsonOptions!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("jsonOptions");
    }

    #endregion

    #region ICursorPaginatedQuery Extension Guards

    [Fact]
    public void ToPaginationOptions_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        ICursorPaginatedQuery<string>? query = null;

        // Act
        var act = () => query!.ToPaginationOptions();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("query");
    }

    [Fact]
    public void IsFirstPage_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        ICursorPaginatedQuery<string>? query = null;

        // Act
        Action act = () => _ = query!.IsFirstPage();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("query");
    }

    #endregion
}
