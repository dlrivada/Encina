using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

using LanguageExt;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance

namespace Encina.ContractTests.Compliance.Anonymization;

/// <summary>
/// Contract tests verifying that <see cref="ITokenMappingStore"/> implementations follow the
/// expected behavioral contract for token mapping lifecycle management.
/// </summary>
public abstract class TokenMappingStoreContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the store being tested.
    /// </summary>
    protected abstract ITokenMappingStore CreateStore();

    #region StoreAsync Contract

    /// <summary>
    /// Contract: StoreAsync with a valid mapping should return Right (success).
    /// </summary>
    [Fact]
    public async Task Contract_StoreAsync_ValidMapping_ReturnsRight()
    {
        var store = CreateStore();
        var mapping = CreateMapping();

        var result = await store.StoreAsync(mapping);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: StoreAsync should preserve all fields when retrieving the stored mapping.
    /// </summary>
    [Fact]
    public async Task Contract_StoreAsync_PreservesAllFields()
    {
        var store = CreateStore();
        var token = $"tok_{Guid.NewGuid():N}";
        var hash = $"hash_{Guid.NewGuid():N}";
        var encrypted = new byte[] { 10, 20, 30, 40, 50 };
        var keyId = "key-2026-02";

        var mapping = TokenMapping.Create(
            token: token,
            originalValueHash: hash,
            encryptedOriginalValue: encrypted,
            keyId: keyId);

        await store.StoreAsync(mapping);

        var result = await store.GetByTokenAsync(token);
        result.IsRight.ShouldBeTrue();

        var option = (Option<TokenMapping>)result;
        option.IsSome.ShouldBeTrue();

        var found = (TokenMapping)option;
        found.Token.ShouldBe(token);
        found.OriginalValueHash.ShouldBe(hash);
        found.EncryptedOriginalValue.ShouldBe(encrypted);
        found.KeyId.ShouldBe(keyId);
    }

    #endregion

    #region GetByTokenAsync Contract

    /// <summary>
    /// Contract: GetByTokenAsync for an existing token should return Some.
    /// </summary>
    [Fact]
    public async Task Contract_GetByTokenAsync_ExistingToken_ReturnsSome()
    {
        var store = CreateStore();
        var mapping = CreateMapping();
        await store.StoreAsync(mapping);

        var result = await store.GetByTokenAsync(mapping.Token);

        result.IsRight.ShouldBeTrue();
        var option = (Option<TokenMapping>)result;
        option.IsSome.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByTokenAsync for a non-existing token should return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetByTokenAsync_NonExisting_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetByTokenAsync("non-existing-token");

        result.IsRight.ShouldBeTrue();
        var option = (Option<TokenMapping>)result;
        option.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetByOriginalValueHashAsync Contract

    /// <summary>
    /// Contract: GetByOriginalValueHashAsync for an existing hash should return Some.
    /// </summary>
    [Fact]
    public async Task Contract_GetByOriginalValueHashAsync_ExistingHash_ReturnsSome()
    {
        var store = CreateStore();
        var mapping = CreateMapping();
        await store.StoreAsync(mapping);

        var result = await store.GetByOriginalValueHashAsync(mapping.OriginalValueHash);

        result.IsRight.ShouldBeTrue();
        var option = (Option<TokenMapping>)result;
        option.IsSome.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByOriginalValueHashAsync for a non-existing hash should return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetByOriginalValueHashAsync_NonExisting_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetByOriginalValueHashAsync("non-existing-hash");

        result.IsRight.ShouldBeTrue();
        var option = (Option<TokenMapping>)result;
        option.IsNone.ShouldBeTrue();
    }

    #endregion

    #region Roundtrip Contract

    /// <summary>
    /// Contract: Storing a mapping and retrieving it by token should return the same data.
    /// </summary>
    [Fact]
    public async Task Contract_StoreAndRetrieve_Roundtrip()
    {
        var store = CreateStore();
        var mapping = CreateMapping();
        await store.StoreAsync(mapping);

        var byTokenResult = await store.GetByTokenAsync(mapping.Token);
        var byTokenOption = (Option<TokenMapping>)byTokenResult;
        var byToken = (TokenMapping)byTokenOption;

        var byHashResult = await store.GetByOriginalValueHashAsync(mapping.OriginalValueHash);
        var byHashOption = (Option<TokenMapping>)byHashResult;
        var byHash = (TokenMapping)byHashOption;

        // Both retrieval methods should return the same mapping
        byToken.Token.ShouldBe(mapping.Token);
        byToken.OriginalValueHash.ShouldBe(mapping.OriginalValueHash);
        byToken.KeyId.ShouldBe(mapping.KeyId);

        byHash.Token.ShouldBe(mapping.Token);
        byHash.OriginalValueHash.ShouldBe(mapping.OriginalValueHash);
        byHash.KeyId.ShouldBe(mapping.KeyId);
    }

    /// <summary>
    /// Contract: Storing multiple mappings should make all of them retrievable.
    /// </summary>
    [Fact]
    public async Task Contract_MultipleStore_AllRetrievable()
    {
        var store = CreateStore();
        var mapping1 = CreateMapping();
        var mapping2 = CreateMapping();
        var mapping3 = CreateMapping();

        await store.StoreAsync(mapping1);
        await store.StoreAsync(mapping2);
        await store.StoreAsync(mapping3);

        var result1 = await store.GetByTokenAsync(mapping1.Token);
        var result2 = await store.GetByTokenAsync(mapping2.Token);
        var result3 = await store.GetByTokenAsync(mapping3.Token);

        result1.IsRight.ShouldBeTrue();
        ((Option<TokenMapping>)result1).IsSome.ShouldBeTrue();

        result2.IsRight.ShouldBeTrue();
        ((Option<TokenMapping>)result2).IsSome.ShouldBeTrue();

        result3.IsRight.ShouldBeTrue();
        ((Option<TokenMapping>)result3).IsSome.ShouldBeTrue();
    }

    #endregion

    #region DeleteByKeyIdAsync Contract

    /// <summary>
    /// Contract: DeleteByKeyIdAsync should remove all mappings for the specified key.
    /// </summary>
    [Fact]
    public async Task Contract_DeleteByKeyIdAsync_ShouldRemoveMappingsForKey()
    {
        var store = CreateStore();
        var mapping1 = CreateMapping(keyId: "key-to-delete");
        var mapping2 = CreateMapping(keyId: "key-to-delete");
        var mappingKeep = CreateMapping(keyId: "key-to-keep");

        await store.StoreAsync(mapping1);
        await store.StoreAsync(mapping2);
        await store.StoreAsync(mappingKeep);

        var deleteResult = await store.DeleteByKeyIdAsync("key-to-delete");
        deleteResult.IsRight.ShouldBeTrue();

        // Deleted mappings should no longer be found
        var result1 = await store.GetByTokenAsync(mapping1.Token);
        ((Option<TokenMapping>)result1).IsNone.ShouldBeTrue();

        var result2 = await store.GetByTokenAsync(mapping2.Token);
        ((Option<TokenMapping>)result2).IsNone.ShouldBeTrue();

        // Mapping with different key should still exist
        var resultKeep = await store.GetByTokenAsync(mappingKeep.Token);
        ((Option<TokenMapping>)resultKeep).IsSome.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: DeleteByKeyIdAsync for a non-existing key should succeed (no-op).
    /// </summary>
    [Fact]
    public async Task Contract_DeleteByKeyIdAsync_NonExistingKey_ShouldSucceed()
    {
        var store = CreateStore();

        var result = await store.DeleteByKeyIdAsync("non-existing-key");

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetAllAsync Contract

    /// <summary>
    /// Contract: GetAllAsync should return all stored mappings.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_ShouldReturnAllMappings()
    {
        var store = CreateStore();
        var mapping1 = CreateMapping();
        var mapping2 = CreateMapping();
        var mapping3 = CreateMapping();

        await store.StoreAsync(mapping1);
        await store.StoreAsync(mapping2);
        await store.StoreAsync(mapping3);

        var result = await store.GetAllAsync();

        result.IsRight.ShouldBeTrue();
        var all = result.RightAsEnumerable().First();
        all.Count.ShouldBe(3);
    }

    /// <summary>
    /// Contract: GetAllAsync on empty store should return empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_EmptyStore_ShouldReturnEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetAllAsync();

        result.IsRight.ShouldBeTrue();
        var all = result.RightAsEnumerable().First();
        all.Count.ShouldBe(0);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a <see cref="TokenMapping"/> with optional overrides for testing.
    /// </summary>
    protected static TokenMapping CreateMapping(
        string? token = null,
        string? hash = null,
        string? keyId = null)
    {
        return TokenMapping.Create(
            token: token ?? Guid.NewGuid().ToString("N"),
            originalValueHash: hash ?? Guid.NewGuid().ToString("N"),
            encryptedOriginalValue: [1, 2, 3],
            keyId: keyId ?? "key-1");
    }

    #endregion
}
