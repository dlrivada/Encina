using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="PersonalDataMap"/> verifying null parameter handling.
/// </summary>
public class PersonalDataMapGuardTests
{
    [Fact]
    public void GetFields_NullEntityType_ThrowsArgumentNullException()
    {
        var map = PersonalDataMap.BuildFromTypes(Array.Empty<Type>());

        var act = () => map.GetFields(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("entityType");
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
}
