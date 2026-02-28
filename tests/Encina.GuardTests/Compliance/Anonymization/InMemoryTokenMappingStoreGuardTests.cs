using Encina.Compliance.Anonymization.InMemory;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="InMemoryTokenMappingStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryTokenMappingStoreGuardTests
{
    private readonly InMemoryTokenMappingStore _store = new();

    /// <summary>
    /// Verifies that StoreAsync throws ArgumentNullException when mapping is null.
    /// </summary>
    [Fact]
    public async Task StoreAsync_NullMapping_ThrowsArgumentNullException()
    {
        var act = async () => await _store.StoreAsync(null!, CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("mapping");
    }

    /// <summary>
    /// Verifies that GetByTokenAsync throws ArgumentNullException when token is null.
    /// </summary>
    [Fact]
    public async Task GetByTokenAsync_NullToken_ThrowsArgumentNullException()
    {
        var act = async () => await _store.GetByTokenAsync(null!, CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("token");
    }

    /// <summary>
    /// Verifies that GetByOriginalValueHashAsync throws ArgumentNullException when hash is null.
    /// </summary>
    [Fact]
    public async Task GetByOriginalValueHashAsync_NullHash_ThrowsArgumentNullException()
    {
        var act = async () => await _store.GetByOriginalValueHashAsync(null!, CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("hash");
    }

    /// <summary>
    /// Verifies that DeleteByKeyIdAsync throws ArgumentNullException when keyId is null.
    /// </summary>
    [Fact]
    public async Task DeleteByKeyIdAsync_NullKeyId_ThrowsArgumentNullException()
    {
        var act = async () => await _store.DeleteByKeyIdAsync(null!, CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("keyId");
    }
}
