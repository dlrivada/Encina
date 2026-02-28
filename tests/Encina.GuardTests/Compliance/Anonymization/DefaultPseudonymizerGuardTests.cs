using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="DefaultPseudonymizer"/> to verify null parameter handling.
/// </summary>
public class DefaultPseudonymizerGuardTests
{
    private sealed class TestDto
    {
        public string Name { get; set; } = "";
    }

    private readonly DefaultPseudonymizer _instance;

    public DefaultPseudonymizerGuardTests()
    {
        _instance = new DefaultPseudonymizer(Substitute.For<IKeyProvider>());
    }

    #region Constructor Guards

    [Fact]
    public void Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultPseudonymizer(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyProvider");
    }

    #endregion

    #region PseudonymizeAsync Guards

    [Fact]
    public async Task PseudonymizeAsync_NullData_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.PseudonymizeAsync<TestDto>(null!, "key-1");

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("data");
    }

    [Fact]
    public async Task PseudonymizeAsync_NullKeyId_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.PseudonymizeAsync(new TestDto(), null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("keyId");
    }

    #endregion

    #region DepseudonymizeAsync Guards

    [Fact]
    public async Task DepseudonymizeAsync_NullData_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.DepseudonymizeAsync<TestDto>(null!, "key-1");

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("data");
    }

    [Fact]
    public async Task DepseudonymizeAsync_NullKeyId_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.DepseudonymizeAsync(new TestDto(), null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("keyId");
    }

    #endregion

    #region PseudonymizeValueAsync Guards

    [Fact]
    public async Task PseudonymizeValueAsync_NullValue_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.PseudonymizeValueAsync(
            null!, "key-1", PseudonymizationAlgorithm.Aes256Gcm);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("value");
    }

    [Fact]
    public async Task PseudonymizeValueAsync_NullKeyId_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.PseudonymizeValueAsync(
            "test-value", null!, PseudonymizationAlgorithm.Aes256Gcm);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("keyId");
    }

    #endregion

    #region DepseudonymizeValueAsync Guards

    [Fact]
    public async Task DepseudonymizeValueAsync_NullPseudonym_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.DepseudonymizeValueAsync(null!, "key-1");

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("pseudonym");
    }

    [Fact]
    public async Task DepseudonymizeValueAsync_NullKeyId_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.DepseudonymizeValueAsync("some-pseudonym", null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("keyId");
    }

    #endregion
}
