using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="DefaultTokenizer"/> to verify null parameter handling.
/// </summary>
public class DefaultTokenizerGuardTests
{
    private static readonly TokenizationOptions ValidOptions = new()
    {
        Format = TokenFormat.Uuid
    };

    private readonly DefaultTokenizer _instance;

    public DefaultTokenizerGuardTests()
    {
        _instance = new DefaultTokenizer(
            Substitute.For<ITokenMappingStore>(),
            Substitute.For<IKeyProvider>());
    }

    #region Constructor Guards

    [Fact]
    public void Constructor_NullMappingStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTokenizer(null!, Substitute.For<IKeyProvider>());

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mappingStore");
    }

    [Fact]
    public void Constructor_NullKeyProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTokenizer(Substitute.For<ITokenMappingStore>(), null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyProvider");
    }

    #endregion

    #region TokenizeAsync Guards

    [Fact]
    public async Task TokenizeAsync_NullValue_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.TokenizeAsync(null!, ValidOptions);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("value");
    }

    [Fact]
    public async Task TokenizeAsync_NullOptions_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.TokenizeAsync("test-value", null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("options");
    }

    #endregion

    #region DetokenizeAsync Guards

    [Fact]
    public async Task DetokenizeAsync_NullToken_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.DetokenizeAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("token");
    }

    #endregion

    #region IsTokenAsync Guards

    [Fact]
    public async Task IsTokenAsync_NullValue_ThrowsArgumentNullException()
    {
        var act = async () => await _instance.IsTokenAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("value");
    }

    #endregion
}
