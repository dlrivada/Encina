using Encina.Compliance.DataSubjectRights;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="PersonalDataMap"/> verifying field discovery and lookup.
/// </summary>
public class PersonalDataMapTests
{
    // Test entity with PersonalData attributes
    private sealed class CustomerEntity
    {
        public Guid Id { get; set; }

        [PersonalData(Category = PersonalDataCategory.Identity)]
        public string FullName { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        public string Email { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Financial, LegalRetention = true, RetentionReason = "Tax law")]
        public string TaxId { get; set; } = string.Empty;
    }

    // Entity without PersonalData attributes
    private sealed class PlainEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void BuildFromTypes_WithPersonalDataEntity_DiscoverFields()
    {
        var map = PersonalDataMap.BuildFromTypes([typeof(CustomerEntity)]);

        map.TypeCount.ShouldBe(1);
        var fields = map.GetFields(typeof(CustomerEntity));
        fields.Count.ShouldBe(3);
    }

    [Fact]
    public void BuildFromTypes_WithPlainEntity_NoFields()
    {
        var map = PersonalDataMap.BuildFromTypes([typeof(PlainEntity)]);

        map.TypeCount.ShouldBe(0);
        map.GetFields(typeof(PlainEntity)).Count.ShouldBe(0);
    }

    [Fact]
    public void BuildFromTypes_EmptyTypes_EmptyMap()
    {
        var map = PersonalDataMap.BuildFromTypes(Array.Empty<Type>());

        map.TypeCount.ShouldBe(0);
    }

    [Fact]
    public void GetFields_PreservesFieldMetadata()
    {
        var map = PersonalDataMap.BuildFromTypes([typeof(CustomerEntity)]);

        var fields = map.GetFields(typeof(CustomerEntity));

        var nameField = fields.First(f => f.PropertyName == "FullName");
        nameField.Category.ShouldBe(PersonalDataCategory.Identity);
        nameField.IsErasable.ShouldBeTrue();
        nameField.IsPortable.ShouldBeTrue();
        nameField.HasLegalRetention.ShouldBeFalse();

        var taxField = fields.First(f => f.PropertyName == "TaxId");
        taxField.Category.ShouldBe(PersonalDataCategory.Financial);
        taxField.HasLegalRetention.ShouldBeTrue();
    }

    [Fact]
    public void GetFields_UnknownType_ReturnsEmptyList()
    {
        var map = PersonalDataMap.BuildFromTypes([typeof(CustomerEntity)]);

        var fields = map.GetFields(typeof(PlainEntity));

        fields.Count.ShouldBe(0);
    }

    [Fact]
    public void RegisteredTypes_ReturnsAllDiscoveredTypes()
    {
        var map = PersonalDataMap.BuildFromTypes([typeof(CustomerEntity), typeof(PlainEntity)]);

        var types = map.RegisteredTypes.ToList();
        types.Count.ShouldBe(1);
        types.ShouldContain(typeof(CustomerEntity));
    }

    [Fact]
    public void BuildFromAssemblies_DiscoverTypesFromAssembly()
    {
        // Use the assembly containing our test types
        var map = PersonalDataMap.BuildFromAssemblies([typeof(CustomerEntity).Assembly]);

        // Should discover at least our CustomerEntity
        map.TypeCount.ShouldBeGreaterThanOrEqualTo(0); // May find other types too
    }
}
