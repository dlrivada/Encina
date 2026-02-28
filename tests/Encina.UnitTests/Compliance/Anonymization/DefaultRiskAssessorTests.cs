#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Time.Testing;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="DefaultRiskAssessor"/>.
/// </summary>
public class DefaultRiskAssessorTests
{
    private sealed class TestRecord
    {
        public string City { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public decimal Salary { get; set; }
    }

    #region Constructor Tests

    [Fact]
    public async Task Constructor_NoArgs_UsesSystemTimeProvider()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cold", Salary = 60000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["City"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.AssessedAtUtc.Should().NotBe(default);
    }

    [Fact]
    public async Task Constructor_FakeTimeProvider_UsesProvidedTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2026, 2, 28, 14, 30, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var sut = new DefaultRiskAssessor(timeProvider);
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cold", Salary = 60000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["City"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.AssessedAtUtc.Should().Be(fixedTime);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void AssessAsync_NullDataset_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();

        // Act
        var act = () => sut.AssessAsync<TestRecord>(null!, ["City"]);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("dataset");
    }

    [Fact]
    public void AssessAsync_NullQuasiIdentifiers_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "LA", Age = 25, Gender = "F", Diagnosis = "Cold", Salary = 60000 },
        };

        // Act
        var act = () => sut.AssessAsync<TestRecord>(dataset, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("quasiIdentifiers");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task AssessAsync_DatasetTooSmall_ReturnsLeftError(int recordCount)
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = Enumerable.Range(0, recordCount)
            .Select(_ => new TestRecord { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 })
            .ToList();

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["City"]);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AssessAsync_EmptyQuasiIdentifiers_ReturnsLeftError()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "LA", Age = 25, Gender = "F", Diagnosis = "Cold", Salary = 60000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, []);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AssessAsync_NoMatchingProperties_ReturnsLeftError()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "LA", Age = 25, Gender = "F", Diagnosis = "Cold", Salary = 60000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["NonExistentProperty", "AnotherFake"]);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Metrics Tests

    [Fact]
    public async Task AssessAsync_AllRecordsIdentical_HighKAnonymity()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cold", Salary = 60000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Headache", Salary = 70000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Fever", Salary = 80000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cough", Salary = 90000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["City", "Age", "Gender"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.KAnonymityValue.Should().Be(5);
    }

    [Fact]
    public async Task AssessAsync_AllRecordsUnique_KAnonymityIsOne()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 25, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "LA", Age = 30, Gender = "F", Diagnosis = "Cold", Salary = 60000 },
            new() { City = "CHI", Age = 35, Gender = "M", Diagnosis = "Flu", Salary = 70000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["City", "Age", "Gender"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.KAnonymityValue.Should().Be(1);
        assessment.IsAcceptable.Should().BeFalse();
    }

    [Fact]
    public async Task AssessAsync_UnacceptableMetrics_ContainsRecommendations()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        // Each record has unique QI values -> k=1 (below target 5)
        // Each class has only 1 sensitive value -> l=1 (below target 3)
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 25, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "LA", Age = 30, Gender = "F", Diagnosis = "Cold", Salary = 60000 },
            new() { City = "CHI", Age = 35, Gender = "M", Diagnosis = "Flu", Salary = 70000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["City", "Age", "Gender"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.Recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AssessAsync_AcceptableMetrics_EmptyRecommendations()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        // All records share the same QI values -> k = 6 (>= 5 target)
        // 6 distinct Diagnosis values -> l >= 3 for sensitive attributes
        // All records in one class -> t-closeness = 0 (class distribution == global distribution)
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 10000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cold", Salary = 20000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Headache", Salary = 30000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Fever", Salary = 40000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cough", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Allergy", Salary = 60000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["City", "Age", "Gender"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.IsAcceptable.Should().BeTrue();
        assessment.Recommendations.Should().BeEmpty();
    }

    [Fact]
    public async Task AssessAsync_ReIdProbability_IsOneOverK()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        // All records share same QI values -> k = dataset.Count = 5
        var dataset = new List<TestRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cold", Salary = 60000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Headache", Salary = 70000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Fever", Salary = 80000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cough", Salary = 90000 },
        };

        // Act
        var result = await sut.AssessAsync<TestRecord>(dataset, ["City", "Age", "Gender"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.ReIdentificationProbability.Should().BeApproximately(1.0 / 5.0, 0.0001);
    }

    #endregion
}
