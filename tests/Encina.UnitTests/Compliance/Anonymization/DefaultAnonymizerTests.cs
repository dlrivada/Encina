#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

using LanguageExt;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="DefaultAnonymizer"/>.
/// </summary>
public class DefaultAnonymizerTests
{
    private sealed class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private static AnonymizationProfile CreateProfile(params FieldAnonymizationRule[] rules) =>
        AnonymizationProfile.Create("test-profile", rules);

    private static IAnonymizationTechnique CreateMockTechnique(
        AnonymizationTechnique technique,
        bool canApply = true)
    {
        var mock = Substitute.For<IAnonymizationTechnique>();
        mock.Technique.Returns(technique);
        mock.CanApply(Arg.Any<Type>()).Returns(canApply);
        return mock;
    }

    #region Constructor

    [Fact]
    public void Constructor_NullTechniques_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DefaultAnonymizer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("techniques");
    }

    #endregion

    #region AnonymizeAsync

    [Fact]
    public async Task AnonymizeAsync_ValidProfileWithMatchingField_AppliesTechniqueToField()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression);
        technique.ApplyAsync(
                Arg.Any<object?>(),
                Arg.Any<Type>(),
                Arg.Any<IReadOnlyDictionary<string, object>?>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>("REDACTED")));

        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression
        });

        // Act
        var result = await anonymizer.AnonymizeAsync(person, profile);

        // Assert
        result.Match(
            Right: v => v.Name.Should().Be("REDACTED"),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AnonymizeAsync_NoMatchingFields_ReturnsOriginalCopy()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression);
        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "NonExistentField",
            Technique = AnonymizationTechnique.Suppression
        });

        // Act
        var result = await anonymizer.AnonymizeAsync(person, profile);

        // Assert
        result.Match(
            Right: v => v.Name.Should().Be("Alice"),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AnonymizeAsync_TechniqueNotRegistered_ReturnsLeftError()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Generalization);
        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression // Not registered
        });

        // Act
        var result = await anonymizer.AnonymizeAsync(person, profile);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AnonymizeAsync_TechniqueCannotApply_ReturnsLeftError()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression, canApply: false);
        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression
        });

        // Act
        var result = await anonymizer.AnonymizeAsync(person, profile);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AnonymizeAsync_TechniqueReturnsLeft_PropagatesError()
    {
        // Arrange
        var expectedError = EncinaError.New("technique failed");
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression);
        technique.ApplyAsync(
                Arg.Any<object?>(),
                Arg.Any<Type>(),
                Arg.Any<IReadOnlyDictionary<string, object>?>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, object?>>(
                Left<EncinaError, object?>(expectedError)));

        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression
        });

        // Act
        var result = await anonymizer.AnonymizeAsync(person, profile);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AnonymizeAsync_DoesNotMutateOriginalObject()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression);
        technique.ApplyAsync(
                Arg.Any<object?>(),
                Arg.Any<Type>(),
                Arg.Any<IReadOnlyDictionary<string, object>?>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>("REDACTED")));

        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression
        });

        // Act
        _ = await anonymizer.AnonymizeAsync(person, profile);

        // Assert
        person.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task AnonymizeAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var anonymizer = new DefaultAnonymizer([]);
        var profile = CreateProfile();

        // Act
        var act = async () => await anonymizer.AnonymizeAsync<TestPerson>(null!, profile);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "data");
    }

    [Fact]
    public async Task AnonymizeAsync_NullProfile_ThrowsArgumentNullException()
    {
        // Arrange
        var anonymizer = new DefaultAnonymizer([]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };

        // Act
        var act = async () => await anonymizer.AnonymizeAsync(person, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "profile");
    }

    #endregion

    #region AnonymizeFieldsAsync

    [Fact]
    public async Task AnonymizeFieldsAsync_ValidProfile_ReturnsCorrectCounts()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression);
        technique.ApplyAsync(
                Arg.Any<object?>(),
                Arg.Any<Type>(),
                Arg.Any<IReadOnlyDictionary<string, object>?>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>("REDACTED")));

        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(
            new FieldAnonymizationRule
            {
                FieldName = "Name",
                Technique = AnonymizationTechnique.Suppression
            },
            new FieldAnonymizationRule
            {
                FieldName = "Email",
                Technique = AnonymizationTechnique.Suppression
            });

        // Act
        var result = await anonymizer.AnonymizeFieldsAsync(person, profile);

        // Assert
        result.Match(
            Right: v => v.AnonymizedFieldCount.Should().Be(2),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AnonymizeFieldsAsync_NoMatchingRules_AllSkipped()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression);
        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "NonExistent",
            Technique = AnonymizationTechnique.Suppression
        });

        // Act
        var result = await anonymizer.AnonymizeFieldsAsync(person, profile);

        // Assert
        result.Match(
            Right: v => v.SkippedFieldCount.Should().Be(v.OriginalFieldCount),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AnonymizeFieldsAsync_TechniqueNotRegistered_ReturnsLeftError()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Generalization);
        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression // Not registered
        });

        // Act
        var result = await anonymizer.AnonymizeFieldsAsync(person, profile);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AnonymizeFieldsAsync_TechniqueCannotApply_SkipsField()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression, canApply: false);
        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression
        });

        // Act
        var result = await anonymizer.AnonymizeFieldsAsync(person, profile);

        // Assert
        result.Match(
            Right: v => v.AnonymizedFieldCount.Should().Be(0),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AnonymizeFieldsAsync_RecordsTechniqueApplied()
    {
        // Arrange
        var technique = CreateMockTechnique(AnonymizationTechnique.Suppression);
        technique.ApplyAsync(
                Arg.Any<object?>(),
                Arg.Any<Type>(),
                Arg.Any<IReadOnlyDictionary<string, object>?>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>("REDACTED")));

        var anonymizer = new DefaultAnonymizer([technique]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };
        var profile = CreateProfile(new FieldAnonymizationRule
        {
            FieldName = "Name",
            Technique = AnonymizationTechnique.Suppression
        });

        // Act
        var result = await anonymizer.AnonymizeFieldsAsync(person, profile);

        // Assert
        result.Match(
            Right: v => v.TechniqueApplied.Should().ContainKey("Name")
                .WhoseValue.Should().Be(AnonymizationTechnique.Suppression),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AnonymizeFieldsAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var anonymizer = new DefaultAnonymizer([]);
        var profile = CreateProfile();

        // Act
        var act = async () => await anonymizer.AnonymizeFieldsAsync<TestPerson>(null!, profile);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "data");
    }

    #endregion

    #region IsAnonymizedAsync

    [Fact]
    public async Task IsAnonymizedAsync_AllFieldsPopulated_ReturnsFalse()
    {
        // Arrange
        var anonymizer = new DefaultAnonymizer([]);
        var person = new TestPerson { Name = "Alice", Email = "alice@test.com", Age = 30 };

        // Act
        var result = await anonymizer.IsAnonymizedAsync(person);

        // Assert
        result.Match(
            Right: v => v.Should().BeFalse(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task IsAnonymizedAsync_MostFieldsNull_ReturnsTrue()
    {
        // Arrange
        var anonymizer = new DefaultAnonymizer([]);
        // Name and Email are null, Age is default (0) => 3/3 fields are null/default => >50%
        var person = new TestPerson { Name = null!, Email = null!, Age = 0 };

        // Act
        var result = await anonymizer.IsAnonymizedAsync(person);

        // Assert
        result.Match(
            Right: v => v.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task IsAnonymizedAsync_StringWithAsterisks_CountsAsMasked()
    {
        // Arrange
        var anonymizer = new DefaultAnonymizer([]);
        // Name has asterisks (masked), Email has asterisks (masked), Age is default (0) => 3/3 => >50%
        var person = new TestPerson { Name = "A****", Email = "***@test.com", Age = 0 };

        // Act
        var result = await anonymizer.IsAnonymizedAsync(person);

        // Assert
        result.Match(
            Right: v => v.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task IsAnonymizedAsync_MixedFields_DeterminesByThreshold()
    {
        // Arrange
        var anonymizer = new DefaultAnonymizer([]);
        // Name is null (counts), Email is populated (does not count), Age is 30 (does not count)
        // 1/3 fields are null/default => NOT >50%
        var person = new TestPerson { Name = null!, Email = "alice@test.com", Age = 30 };

        // Act
        var result = await anonymizer.IsAnonymizedAsync(person);

        // Assert
        result.Match(
            Right: v => v.Should().BeFalse(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task IsAnonymizedAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var anonymizer = new DefaultAnonymizer([]);

        // Act
        var act = async () => await anonymizer.IsAnonymizedAsync<TestPerson>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "data");
    }

    #endregion
}
