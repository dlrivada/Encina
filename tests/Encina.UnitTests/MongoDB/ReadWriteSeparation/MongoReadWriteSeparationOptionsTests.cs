using Encina.MongoDB.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.MongoDB.ReadWriteSeparation;

public sealed class MongoReadWriteSeparationOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new MongoReadWriteSeparationOptions();

        // Assert
        options.ReadPreference.ShouldBe(MongoReadPreference.SecondaryPreferred);
        options.ReadConcern.ShouldBe(MongoReadConcern.Majority);
        options.ValidateOnStartup.ShouldBeFalse();
        options.FallbackToPrimaryOnNoSecondaries.ShouldBeTrue();
        options.MaxStaleness.ShouldBeNull();
    }

    [Theory]
    [InlineData(MongoReadPreference.Primary)]
    [InlineData(MongoReadPreference.PrimaryPreferred)]
    [InlineData(MongoReadPreference.Secondary)]
    [InlineData(MongoReadPreference.SecondaryPreferred)]
    [InlineData(MongoReadPreference.Nearest)]
    public void ReadPreference_CanBeSet(MongoReadPreference preference)
    {
        // Arrange
        var options = new MongoReadWriteSeparationOptions();

        // Act
        options.ReadPreference = preference;

        // Assert
        options.ReadPreference.ShouldBe(preference);
    }

    [Theory]
    [InlineData(MongoReadConcern.Default)]
    [InlineData(MongoReadConcern.Local)]
    [InlineData(MongoReadConcern.Majority)]
    [InlineData(MongoReadConcern.Linearizable)]
    [InlineData(MongoReadConcern.Available)]
    [InlineData(MongoReadConcern.Snapshot)]
    public void ReadConcern_CanBeSet(MongoReadConcern concern)
    {
        // Arrange
        var options = new MongoReadWriteSeparationOptions();

        // Act
        options.ReadConcern = concern;

        // Assert
        options.ReadConcern.ShouldBe(concern);
    }

    [Fact]
    public void ValidateOnStartup_CanBeEnabled()
    {
        // Arrange
        var options = new MongoReadWriteSeparationOptions();

        // Act
        options.ValidateOnStartup = true;

        // Assert
        options.ValidateOnStartup.ShouldBeTrue();
    }

    [Fact]
    public void FallbackToPrimaryOnNoSecondaries_CanBeDisabled()
    {
        // Arrange
        var options = new MongoReadWriteSeparationOptions();

        // Act
        options.FallbackToPrimaryOnNoSecondaries = false;

        // Assert
        options.FallbackToPrimaryOnNoSecondaries.ShouldBeFalse();
    }

    [Fact]
    public void MaxStaleness_CanBeSet()
    {
        // Arrange
        var options = new MongoReadWriteSeparationOptions();
        var staleness = TimeSpan.FromMinutes(5);

        // Act
        options.MaxStaleness = staleness;

        // Assert
        options.MaxStaleness.ShouldBe(staleness);
    }

    [Fact]
    public void MaxStaleness_CanBeCleared()
    {
        // Arrange
        var options = new MongoReadWriteSeparationOptions
        {
            MaxStaleness = TimeSpan.FromMinutes(5)
        };

        // Act
        options.MaxStaleness = null;

        // Assert
        options.MaxStaleness.ShouldBeNull();
    }
}
