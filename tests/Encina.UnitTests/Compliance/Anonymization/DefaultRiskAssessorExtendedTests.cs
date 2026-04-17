#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Extended unit tests for <see cref="DefaultRiskAssessor"/> covering edge cases
/// and deeper metric calculations beyond the base test class.
/// </summary>
public class DefaultRiskAssessorExtendedTests
{
    private sealed class PersonRecord
    {
        public string City { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string ZipCode { get; set; } = string.Empty;
    }

    #region Identical Records (K-Anonymity = Full Count)

    [Fact]
    public async Task AssessAsync_IdenticalRecords_KAnonymityEqualsDatasetCount()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = Enumerable.Range(0, 10)
            .Select(_ => new PersonRecord
            {
                City = "Madrid",
                Age = 40,
                Gender = "F",
                Diagnosis = "Flu",
                Salary = 50000,
                ZipCode = "28001"
            })
            .ToList();

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City", "Age", "Gender"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.KAnonymityValue.ShouldBe(10, "all 10 records share the same QI values");
        _ = assessment.ReIdentificationProbability;
    }

    [Fact]
    public async Task AssessAsync_IdenticalRecordsWithDiverseSensitiveAttrs_HighLDiversity()
    {
        // Arrange — all non-QI properties vary to ensure minimum l-diversity >= 3
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu",       Salary = 40000, ZipCode = "10001" },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Cold",      Salary = 50000, ZipCode = "10002" },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Migraine",  Salary = 60000, ZipCode = "10003" },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Asthma",    Salary = 70000, ZipCode = "10004" },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Bronchitis",Salary = 80000, ZipCode = "10005" },
        };

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City", "Age", "Gender"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.KAnonymityValue.ShouldBe(5);
        // L-diversity is the minimum distinct values for ANY sensitive property
        // Diagnosis: 5, Salary: 5, ZipCode: 5 → min = 5
        assessment.LDiversityValue.ShouldBeGreaterThanOrEqualTo(3,
            "all sensitive properties have 5 distinct values within the single equivalence class");
    }

    #endregion

    #region Empty and Small Dataset Errors

    [Fact]
    public async Task AssessAsync_EmptyDataset_ReturnsLeftError()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>();

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City"]);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("at least 2 records");
    }

    [Fact]
    public async Task AssessAsync_SingleRecord_ReturnsLeftError()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>
        {
            new() { City = "London", Age = 28, Gender = "F", Diagnosis = "Flu", Salary = 45000 }
        };

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City"]);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Large Dataset with All Metric Calculations

    [Fact]
    public async Task AssessAsync_LargeDataset_ExercisesAllMetrics()
    {
        // Arrange — 20 records with 4 equivalence classes of 5 records each
        // ALL sensitive (non-QI) properties vary within each class for l-diversity
        var sut = new DefaultRiskAssessor();
        var cities = new[] { "NYC", "LA", "CHI", "HOU" };
        var dataset = new List<PersonRecord>();

        for (var cityIdx = 0; cityIdx < cities.Length; cityIdx++)
        {
            for (var i = 0; i < 5; i++)
            {
                dataset.Add(new PersonRecord
                {
                    City = cities[cityIdx],
                    Age = 30 + cityIdx,
                    Gender = i % 2 == 0 ? "M" : "F",
                    Diagnosis = $"Diagnosis-{cityIdx}-{i}",
                    Salary = 40000 + (i * 10000),
                    ZipCode = $"1000{(cityIdx * 5) + i}"
                });
            }
        }

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City", "Age"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.KAnonymityValue.ShouldBe(5, "each city+age group has exactly 5 records");
        // Gender has only 2 distinct values (M/F) per class → l-diversity min = 2
        assessment.LDiversityValue.ShouldBeGreaterThanOrEqualTo(2,
            "gender has 2 distinct values per class");
        assessment.TClosenessDistance.ShouldBeGreaterThanOrEqualTo(0.0);
        _ = assessment.ReIdentificationProbability;
        // k=5 meets target, but l=2 is below target 3 → not acceptable
        assessment.IsAcceptable.ShouldBeFalse();
        assessment.Recommendations.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task AssessAsync_LargeDataset_HighTCloseness_IsNotAcceptable()
    {
        // Arrange — create skewed distribution within classes
        // Class 1: all "Flu", Class 2: all "Cancer" → high t-closeness distance
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>();

        // 5 records in class "NYC/30" all with Flu
        for (var i = 0; i < 5; i++)
        {
            dataset.Add(new PersonRecord
            {
                City = "NYC",
                Age = 30,
                Gender = "M",
                Diagnosis = "Flu",
                Salary = 50000 + (i * 1000),
                ZipCode = "10001"
            });
        }

        // 5 records in class "LA/40" all with Cancer
        for (var i = 0; i < 5; i++)
        {
            dataset.Add(new PersonRecord
            {
                City = "LA",
                Age = 40,
                Gender = "F",
                Diagnosis = "Cancer",
                Salary = 60000 + (i * 1000),
                ZipCode = "90001"
            });
        }

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City", "Age"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.KAnonymityValue.ShouldBe(5);
        // Diagnosis within each class is homogeneous → l-diversity = 1
        assessment.LDiversityValue.ShouldBe(1);
        assessment.TClosenessDistance.ShouldBeGreaterThan(0.0,
            "skewed diagnosis distribution within classes should produce positive t-closeness");
        assessment.IsAcceptable.ShouldBeFalse();
    }

    #endregion

    #region Multiple Quasi-Identifiers

    [Fact]
    public async Task AssessAsync_SingleQuasiIdentifier_CalculatesCorrectly()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>
        {
            new() { City = "NYC", Age = 25, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "F", Diagnosis = "Cold", Salary = 60000 },
            new() { City = "LA", Age = 35, Gender = "M", Diagnosis = "Asthma", Salary = 70000 },
        };

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        // NYC has 2 records, LA has 1 → k=1
        assessment.KAnonymityValue.ShouldBe(1);
    }

    [Fact]
    public async Task AssessAsync_AllPropertiesAsQuasiIdentifiers_MaximumGranularity()
    {
        // Arrange — using many QI reduces equivalence class sizes
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>
        {
            new() { City = "NYC", Age = 25, Gender = "M", Diagnosis = "Flu", Salary = 50000, ZipCode = "10001" },
            new() { City = "NYC", Age = 25, Gender = "M", Diagnosis = "Cold", Salary = 60000, ZipCode = "10001" },
            new() { City = "NYC", Age = 30, Gender = "F", Diagnosis = "Flu", Salary = 70000, ZipCode = "10002" },
        };

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City", "Age", "Gender", "ZipCode"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        // NYC/25/M/10001 → 2 records, NYC/30/F/10002 → 1 record → k=1
        assessment.KAnonymityValue.ShouldBe(1);
    }

    [Fact]
    public async Task AssessAsync_TwoQuasiIdentifiers_PartitionsCorrectly()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "A", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "F", Diagnosis = "B", Salary = 60000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "C", Salary = 70000 },
            new() { City = "LA", Age = 40, Gender = "F", Diagnosis = "D", Salary = 80000 },
            new() { City = "LA", Age = 40, Gender = "M", Diagnosis = "E", Salary = 90000 },
        };

        // Act — City+Age as QI
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City", "Age"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        // NYC/30 → 3 records, LA/40 → 2 records → k=2
        assessment.KAnonymityValue.ShouldBe(2);
    }

    #endregion

    #region Non-Matching Quasi-Identifiers

    [Fact]
    public async Task AssessAsync_MixedValidAndInvalidQI_OnlyUsesValidOnes()
    {
        // Arrange
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "Flu", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "F", Diagnosis = "Cold", Salary = 60000 },
        };

        // Act — "City" is valid, "NonExistent" is not
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City", "NonExistent"]);

        // Assert — should still work using only "City"
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.KAnonymityValue.ShouldBe(2, "both records share City=NYC");
    }

    #endregion

    #region T-Closeness Edge Cases

    [Fact]
    public async Task AssessAsync_SingleEquivalenceClass_TClosenessIsZero()
    {
        // Arrange — all records in same class → class distribution == global distribution
        var sut = new DefaultRiskAssessor();
        var dataset = new List<PersonRecord>
        {
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "A", Salary = 50000 },
            new() { City = "NYC", Age = 30, Gender = "F", Diagnosis = "B", Salary = 60000 },
            new() { City = "NYC", Age = 30, Gender = "M", Diagnosis = "C", Salary = 70000 },
        };

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City", "Age"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.TClosenessDistance.ShouldBe(0.0,
            "single equivalence class means class distribution matches global distribution exactly");
    }

    #endregion

    #region Timestamp Verification

    [Fact]
    public async Task AssessAsync_UsesTimeProviderForTimestamp()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var sut = new DefaultRiskAssessor(timeProvider);
        var dataset = new List<PersonRecord>
        {
            new() { City = "A", Age = 20, Gender = "M", Diagnosis = "X", Salary = 1000 },
            new() { City = "B", Age = 30, Gender = "F", Diagnosis = "Y", Salary = 2000 },
        };

        // Act
        var result = await sut.AssessAsync<PersonRecord>(dataset, ["City"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(Right: r => r, Left: _ => null!);
        assessment.AssessedAtUtc.ShouldBe(fixedTime);
    }

    #endregion
}
