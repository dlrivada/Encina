using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class CryptoShreddedPropertyCacheTests : IDisposable
{
    public void Dispose()
    {
        CryptoShreddedPropertyCache.ClearCache();
    }

    [Fact]
    public void GetFields_TypeWithCryptoShredded_ReturnsFields()
    {
        // Act
        var fields = CryptoShreddedPropertyCache.GetFields(typeof(TestEventWithPii));

        // Assert
        fields.ShouldNotBeEmpty();
        fields.Length.ShouldBe(1);
        fields[0].Property.Name.ShouldBe(nameof(TestEventWithPii.Email));
        fields[0].SubjectIdProperty.ShouldBe(nameof(TestEventWithPii.UserId));
    }

    [Fact]
    public void GetFields_TypeWithoutCryptoShredded_ReturnsEmpty()
    {
        // Act
        var fields = CryptoShreddedPropertyCache.GetFields(typeof(TestEventNoPii));

        // Assert
        fields.ShouldBeEmpty();
    }

    [Fact]
    public void GetFields_CachesResult()
    {
        // Act
        var first = CryptoShreddedPropertyCache.GetFields(typeof(TestEventWithPii));
        var second = CryptoShreddedPropertyCache.GetFields(typeof(TestEventWithPii));

        // Assert
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void HasCryptoShreddedFields_TypeWithFields_ReturnsTrue()
    {
        // Act
        var result = CryptoShreddedPropertyCache.HasCryptoShreddedFields(typeof(TestEventWithPii));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasCryptoShreddedFields_TypeWithoutFields_ReturnsFalse()
    {
        // Act
        var result = CryptoShreddedPropertyCache.HasCryptoShreddedFields(typeof(TestEventNoPii));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetFields_MissingPersonalDataAttribute_ExcludesProperty()
    {
        // Act — MissingPersonalData has [CryptoShredded] but not [PersonalData]
        var fields = CryptoShreddedPropertyCache.GetFields(typeof(TestEventMissingPersonalData));

        // Assert
        fields.ShouldBeEmpty();
    }

    [Fact]
    public void GetFields_InvalidSubjectIdProperty_ExcludesProperty()
    {
        // Act
        var fields = CryptoShreddedPropertyCache.GetFields(typeof(TestEventInvalidSubjectId));

        // Assert
        fields.ShouldBeEmpty();
    }

    [Fact]
    public void GetFields_NonStringProperty_ExcludesProperty()
    {
        // Act
        var fields = CryptoShreddedPropertyCache.GetFields(typeof(TestEventNonStringPii));

        // Assert
        fields.ShouldBeEmpty();
    }

    [Fact]
    public void CompiledSetter_SetsValue()
    {
        // Arrange
        var fields = CryptoShreddedPropertyCache.GetFields(typeof(TestEventWithPii));
        var instance = new TestEventWithPii { UserId = "user-1", Email = "original@test.com" };

        // Act
        fields[0].SetValue(instance, "encrypted-value");

        // Assert
        instance.Email.ShouldBe("encrypted-value");
    }

    [Fact]
    public void GetValue_ReadsValue()
    {
        // Arrange
        var fields = CryptoShreddedPropertyCache.GetFields(typeof(TestEventWithPii));
        var instance = new TestEventWithPii { UserId = "user-1", Email = "test@example.com" };

        // Act
        var value = fields[0].GetValue(instance);

        // Assert
        value.ShouldBe("test@example.com");
    }

    [Fact]
    public void HasAnyRegisteredTypes_AfterDiscovery_ReturnsTrue()
    {
        // Act
        CryptoShreddedPropertyCache.GetFields(typeof(TestEventWithPii));

        // Assert
        CryptoShreddedPropertyCache.HasAnyRegisteredTypes.ShouldBeTrue();
    }

    [Fact]
    public void ClearCache_RemovesAllEntries()
    {
        // Arrange
        CryptoShreddedPropertyCache.GetFields(typeof(TestEventWithPii));
        CryptoShreddedPropertyCache.HasAnyRegisteredTypes.ShouldBeTrue();

        // Act
        CryptoShreddedPropertyCache.ClearCache();

        // Assert
        CryptoShreddedPropertyCache.HasAnyRegisteredTypes.ShouldBeFalse();
        CryptoShreddedPropertyCache.CachedTypeCount.ShouldBe(0);
    }

    [Fact]
    public void GetFields_MultipleProperties_ReturnsAll()
    {
        // Act
        var fields = CryptoShreddedPropertyCache.GetFields(typeof(TestEventMultiplePii));

        // Assert
        fields.Length.ShouldBe(2);
    }

    // Test types

    public class TestEventWithPii
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public class TestEventNoPii
    {
        public string UserId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
    }

    public class TestEventMissingPersonalData
    {
        public string UserId { get; set; } = string.Empty;

        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public class TestEventInvalidSubjectId
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = "NonExistentProperty")]
        public string Email { get; set; } = string.Empty;
    }

    public class TestEventNonStringPii
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Other, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public int Age { get; set; }
    }

    public class TestEventMultiplePii
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Phone { get; set; } = string.Empty;
    }
}
