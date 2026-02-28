using Encina.Compliance.Anonymization.InMemory;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="InMemoryKeyProvider"/> to verify null parameter handling.
/// </summary>
public class InMemoryKeyProviderGuardTests
{
    private readonly InMemoryKeyProvider _provider = new();

    /// <summary>
    /// Verifies that GetKeyAsync throws ArgumentNullException when keyId is null.
    /// </summary>
    [Fact]
    public async Task GetKeyAsync_NullKeyId_ThrowsArgumentNullException()
    {
        var act = async () => await _provider.GetKeyAsync(null!, CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("keyId");
    }

    /// <summary>
    /// Verifies that RotateKeyAsync throws ArgumentNullException when keyId is null.
    /// </summary>
    [Fact]
    public async Task RotateKeyAsync_NullKeyId_ThrowsArgumentNullException()
    {
        var act = async () => await _provider.RotateKeyAsync(null!, CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("keyId");
    }
}
