using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="DefaultAnonymizer"/> to verify null parameter handling.
/// </summary>
public class DefaultAnonymizerGuardTests
{
    private sealed class TestDto
    {
        public string Name { get; set; } = "";
    }

    private static readonly AnonymizationProfile ValidProfile = AnonymizationProfile.Create(
        "test",
        [new FieldAnonymizationRule { FieldName = "Name", Technique = AnonymizationTechnique.Suppression }]);

    private readonly DefaultAnonymizer _instance;

    public DefaultAnonymizerGuardTests()
    {
        var technique = Substitute.For<IAnonymizationTechnique>();
        _instance = new DefaultAnonymizer([technique]);
    }

    #region Constructor Guards

    [Fact]
    public void Constructor_NullTechniques_ThrowsArgumentNullException()
    {
        var act = () => new DefaultAnonymizer(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("techniques");
    }

    #endregion

    #region AnonymizeAsync Guards

    [Fact]
    public async Task AnonymizeAsync_NullData_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.AnonymizeAsync<TestDto>(null!, ValidProfile);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("data");
    }

    [Fact]
    public async Task AnonymizeAsync_NullProfile_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.AnonymizeAsync(new TestDto(), null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("profile");
    }

    #endregion

    #region AnonymizeFieldsAsync Guards

    [Fact]
    public async Task AnonymizeFieldsAsync_NullData_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.AnonymizeFieldsAsync<TestDto>(null!, ValidProfile);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("data");
    }

    [Fact]
    public async Task AnonymizeFieldsAsync_NullProfile_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.AnonymizeFieldsAsync(new TestDto(), null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("profile");
    }

    #endregion

    #region IsAnonymizedAsync Guards

    [Fact]
    public async Task IsAnonymizedAsync_NullData_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.IsAnonymizedAsync<TestDto>(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("data");
    }

    #endregion
}
