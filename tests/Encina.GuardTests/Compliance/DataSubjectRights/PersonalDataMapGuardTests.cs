using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="PersonalDataMap"/> verifying null parameter handling
/// and basic construction behavior.
/// </summary>
public class PersonalDataMapGuardTests
{
    [Fact]
    public void Constructor_NullMap_ThrowsArgumentNullException()
    {
        var act = () => new PersonalDataMap(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("map");
    }

    [Fact]
    public void GetFields_NullEntityType_ThrowsArgumentNullException()
    {
        var map = PersonalDataMap.BuildFromTypes(Array.Empty<Type>());

        var act = () => map.GetFields(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("entityType");
    }

    [Fact]
    public void GetFields_UnknownType_ReturnsEmptyList()
    {
        var map = PersonalDataMap.BuildFromTypes(Array.Empty<Type>());

        var result = map.GetFields(typeof(string));

        result.ShouldBeEmpty();
    }

    [Fact]
    public void BuildFromAssemblies_NullAssemblies_ThrowsArgumentNullException()
    {
        var act = () => PersonalDataMap.BuildFromAssemblies(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("assemblies");
    }

    [Fact]
    public void BuildFromTypes_NullTypes_ThrowsArgumentNullException()
    {
        var act = () => PersonalDataMap.BuildFromTypes(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("types");
    }

    [Fact]
    public void BuildFromTypes_EmptyTypes_ReturnsEmptyMap()
    {
        var map = PersonalDataMap.BuildFromTypes(Array.Empty<Type>());

        map.TypeCount.ShouldBe(0);
        map.RegisteredTypes.ShouldBeEmpty();
    }

    [Fact]
    public void BuildFromAssemblies_EmptyAssemblies_ReturnsEmptyMap()
    {
        var map = PersonalDataMap.BuildFromAssemblies([]);

        map.TypeCount.ShouldBe(0);
    }
}
