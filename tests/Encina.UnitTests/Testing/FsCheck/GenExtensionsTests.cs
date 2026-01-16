using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Testing.FsCheck;

/// <summary>
/// Unit tests for <see cref="GenExtensions"/>.
/// </summary>
public class GenExtensionsTests
{
    // FsCheck 3.x auto-discovers arbitraries via EncinaArbitraryProvider

    #region ToEither Tests

    [Fact]
    public void ToEither_GeneratesBothLeftAndRight()
    {
        // Arrange
        var gen = ArbMap.Default.GeneratorFor<int>().ToEither();

        // Act
        var samples = Gen.Sample(gen, 10, 100).ToList();

        // Assert
        samples.Any(e => e.IsLeft).ShouldBeTrue("Should generate at least one Left");
        samples.Any(e => e.IsRight).ShouldBeTrue("Should generate at least one Right");
    }

    [Fact]
    public void ToSuccess_GeneratesOnlyRight()
    {
        // Arrange
        var gen = ArbMap.Default.GeneratorFor<int>().ToSuccess();

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(e => e.IsRight);
    }

    [Fact]
    public void ToFailure_GeneratesOnlyLeft()
    {
        // Arrange
        var gen = EncinaArbitraries.EncinaError().Generator.ToFailure<string>();

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(e => e.IsLeft);
    }

    #endregion

    #region OrNull Tests

    [Fact]
    public void OrNull_GeneratesSomeNulls()
    {
        // Arrange
        var gen = GenExtensions.NonEmptyString().OrNull(0.5);

        // Act
        var samples = Gen.Sample(gen, 10, 100).ToList();

        // Assert
        samples.Any(s => s == null).ShouldBeTrue("Should generate at least one null");
        samples.Any(s => s != null).ShouldBeTrue("Should generate at least one non-null");
    }

    [Fact]
    public void OrNull_WithZeroProbability_GeneratesNoNulls()
    {
        // Arrange
        var gen = GenExtensions.NonEmptyString().OrNull(0.0);

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(s => s != null);
    }

    [Fact]
    public void OrNullValue_GeneratesSomeNulls()
    {
        // Arrange
        var gen = ArbMap.Default.GeneratorFor<int>().OrNullValue(0.5);

        // Act
        var samples = Gen.Sample(gen, 10, 100).ToList();

        // Assert
        samples.Any(s => s == null).ShouldBeTrue("Should generate at least one null");
        samples.Any(s => s != null).ShouldBeTrue("Should generate at least one non-null");
    }

    #endregion

    #region String Generators Tests

    [Fact]
    public void NonEmptyString_GeneratesNonEmptyStrings()
    {
        // Arrange
        var gen = GenExtensions.NonEmptyString();

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(s => !string.IsNullOrEmpty(s));
    }

    [Fact]
    public void AlphaNumericString_GeneratesValidStrings()
    {
        // Arrange
        var gen = GenExtensions.AlphaNumericString(5, 10);

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(s => s.Length >= 5 && s.Length <= 10);
        samples.ShouldAllBe(s => s.All(c => char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void EmailAddress_GeneratesValidEmails()
    {
        // Arrange
        var gen = GenExtensions.EmailAddress();

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(s => s.Contains('@'));
        samples.ShouldAllBe(s => s.Contains('.'));
    }

    #endregion

    #region JSON Generators Tests

    [Fact]
    public void JsonObject_GeneratesValidJson()
    {
        // Arrange
        var gen = GenExtensions.JsonObject(3);

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(s => s.StartsWith('{'));
        samples.ShouldAllBe(s => s.EndsWith('}'));
    }

    [Fact]
    public void JsonObject_WithZeroProperties_GeneratesEmptyObject()
    {
        // Arrange
        var gen = GenExtensions.JsonObject(0);

        // Act
        var samples = Gen.Sample(gen, 10, 20).ToList();

        // Assert
        samples.ShouldAllBe(s => s == "{}");
    }

    #endregion

    #region DateTime Generators Tests

    [Fact]
    public void UtcDateTime_GeneratesValidDates()
    {
        // Arrange
        var gen = GenExtensions.UtcDateTime(30);

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        var now = DateTime.UtcNow;
        samples.ShouldAllBe(d => d >= now.AddDays(-31) && d <= now.AddDays(31));
    }

    [Fact]
    public void PastUtcDateTime_GeneratesPastDates()
    {
        // Arrange
        var gen = GenExtensions.PastUtcDateTime(30);
        var now = DateTime.UtcNow; // Capture before sampling to avoid race condition

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(d => d < now);
    }

    [Fact]
    public void FutureUtcDateTime_GeneratesFutureDates()
    {
        // Arrange
        var gen = GenExtensions.FutureUtcDateTime(30);
        var now = DateTime.UtcNow; // Capture before sampling to avoid race condition

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(d => d > now);
    }

    #endregion

    #region Cron Expression Tests

    [Fact]
    public void CronExpression_GeneratesValidExpressions()
    {
        // Arrange
        var gen = GenExtensions.CronExpression();

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(s => s.Split(' ').Length == 5);
    }

    #endregion

    #region Collection Generators Tests

    [Fact]
    public void ListOf_GeneratesListsWithinBounds()
    {
        // Arrange
        var gen = ArbMap.Default.GeneratorFor<int>().ListOf(2, 5);

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(list => list.Count >= 2 && list.Count <= 5);
    }

    [Fact]
    public void NonEmptyListOf_GeneratesNonEmptyLists()
    {
        // Arrange
        var gen = ArbMap.Default.GeneratorFor<int>().NonEmptyListOf(5);

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(list => list.Count >= 1);
    }

    #endregion

    #region PositiveDecimal Tests

    [Fact]
    public void PositiveDecimal_GeneratesPositiveValues()
    {
        // Arrange
        var gen = GenExtensions.PositiveDecimal(0.01m, 100m);

        // Act
        var samples = Gen.Sample(gen, 10, 50).ToList();

        // Assert
        samples.ShouldAllBe(d => d >= 0.01m && d <= 100m);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void OrNull_ThrowsForInvalidProbability()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            GenExtensions.NonEmptyString().OrNull(-0.1));

        Should.Throw<ArgumentOutOfRangeException>(() =>
            GenExtensions.NonEmptyString().OrNull(1.1));
    }

    [Fact]
    public void AlphaNumericString_ThrowsForInvalidLengths()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            GenExtensions.AlphaNumericString(-1, 10));

        Should.Throw<ArgumentOutOfRangeException>(() =>
            GenExtensions.AlphaNumericString(10, 5));
    }

    #endregion
}
