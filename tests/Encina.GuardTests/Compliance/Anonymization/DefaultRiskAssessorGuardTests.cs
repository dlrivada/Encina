using Encina.Compliance.Anonymization;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="DefaultRiskAssessor"/> to verify null parameter handling.
/// </summary>
public class DefaultRiskAssessorGuardTests
{
    private sealed class TestRecord
    {
        public string City { get; set; } = "";
    }

    private readonly DefaultRiskAssessor _instance;

    public DefaultRiskAssessorGuardTests()
    {
        _instance = new DefaultRiskAssessor();
    }

    #region AssessAsync Guards

    [Fact]
    public async Task AssessAsync_NullDataset_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.AssessAsync<TestRecord>(null!, ["City"]);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("dataset");
    }

    [Fact]
    public async Task AssessAsync_NullQuasiIdentifiers_ThrowsArgumentNullException()
    {
        var dataset = new List<TestRecord>
        {
            new() { City = "Madrid" },
            new() { City = "Barcelona" }
        };

        var act = async () => await _instance.AssessAsync(dataset, null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("quasiIdentifiers");
    }

    #endregion
}
