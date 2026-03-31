using Encina.EntityFrameworkCore.Converters;
using Encina.IdGeneration;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Converters;

/// <summary>
/// Unit tests for <see cref="UuidV7IdConverter"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class UuidV7IdConverterTests
{
    private readonly UuidV7IdConverter _sut = new();

    #region Round-Trip Conversion

    [Fact]
    public void RoundTrip_NewUuidV7_PreservesValue()
    {
        // Arrange
        var original = UuidV7Id.NewUuidV7();

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
    }

    [Fact]
    public void RoundTrip_EmptyGuid_PreservesValue()
    {
        // Arrange
        var original = new UuidV7Id(Guid.Empty);

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
        restored.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void RoundTrip_KnownGuid_PreservesValue()
    {
        // Arrange
        var guid = Guid.Parse("019374c8-7b00-7000-8000-000000000001");
        var original = new UuidV7Id(guid);

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
        restored.Value.ShouldBe(guid);
    }

    [Fact]
    public void RoundTrip_MultipleIds_AllPreserved()
    {
        for (var i = 0; i < 10; i++)
        {
            // Arrange
            var original = UuidV7Id.NewUuidV7();

            // Act
            var dbValue = ConvertToProvider(original);
            var restored = ConvertFromProvider(dbValue);

            // Assert
            restored.ShouldBe(original);
        }
    }

    #endregion

    #region To Provider (UuidV7Id -> Guid)

    [Fact]
    public void ConvertToProvider_ReturnsUnderlyingGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new UuidV7Id(guid);

        // Act
        var result = ConvertToProvider(id);

        // Assert
        result.ShouldBe(guid);
    }

    [Fact]
    public void ConvertToProvider_EmptyId_ReturnsEmptyGuid()
    {
        // Arrange
        var id = UuidV7Id.Empty;

        // Act
        var result = ConvertToProvider(id);

        // Assert
        result.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void ConvertToProvider_NewUuidV7_ReturnsNonEmptyGuid()
    {
        // Arrange
        var id = UuidV7Id.NewUuidV7();

        // Act
        var result = ConvertToProvider(id);

        // Assert
        result.ShouldNotBe(Guid.Empty);
    }

    #endregion

    #region From Provider (Guid -> UuidV7Id)

    [Fact]
    public void ConvertFromProvider_ValidGuid_CreatesCorrectId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = ConvertFromProvider(guid);

        // Assert
        result.Value.ShouldBe(guid);
        result.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void ConvertFromProvider_EmptyGuid_CreatesEmptyId()
    {
        // Act
        var result = ConvertFromProvider(Guid.Empty);

        // Assert
        result.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void ConvertFromProvider_UuidV7Guid_PreservesTimestamp()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var id = UuidV7Id.NewUuidV7(now);
        var guid = id.Value;

        // Act
        var restored = ConvertFromProvider(guid);

        // Assert
        var restoredTimestamp = restored.GetTimestamp();
        // Timestamp should be within 1 second tolerance (millisecond precision)
        Math.Abs((restoredTimestamp - now).TotalMilliseconds).ShouldBeLessThan(1000);
    }

    #endregion

    #region Converter Instantiation

    [Fact]
    public void Constructor_CreatesValidConverter()
    {
        var converter = new UuidV7IdConverter();
        converter.ShouldNotBeNull();
    }

    #endregion

    #region Helpers

    private Guid ConvertToProvider(UuidV7Id id)
    {
        var convertToProvider = _sut.ConvertToProviderExpression.Compile();
        return convertToProvider(id);
    }

    private UuidV7Id ConvertFromProvider(Guid value)
    {
        var convertFromProvider = _sut.ConvertFromProviderExpression.Compile();
        return convertFromProvider(value);
    }

    #endregion
}
