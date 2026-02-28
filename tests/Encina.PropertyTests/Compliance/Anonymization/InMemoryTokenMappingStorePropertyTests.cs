using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using LanguageExt;

namespace Encina.PropertyTests.Compliance.Anonymization;

/// <summary>
/// Property-based tests for <see cref="InMemoryTokenMappingStore"/> verifying store
/// invariants using FsCheck random data generation.
/// </summary>
public class InMemoryTokenMappingStorePropertyTests
{
    #region Store Roundtrip Invariants

    /// <summary>
    /// Invariant: Any stored mapping can always be retrieved by its token.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Store_ThenGetByToken_AlwaysReturnsStoredMapping(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var store = new InMemoryTokenMappingStore();
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 1, 2, 3 },
            keyId: keyId.Get);

        var storeResult = store.StoreAsync(mapping).AsTask().Result;
        if (!storeResult.IsRight) return false;

        var result = store.GetByTokenAsync(token.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var option = (Option<TokenMapping>)result;
        return option.Match(
            Some: m => m.Token == token.Get,
            None: () => false);
    }

    /// <summary>
    /// Invariant: Any stored mapping can always be retrieved by its original value hash.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Store_ThenGetByHash_AlwaysReturnsStoredMapping(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var store = new InMemoryTokenMappingStore();
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 4, 5, 6 },
            keyId: keyId.Get);

        var storeResult = store.StoreAsync(mapping).AsTask().Result;
        if (!storeResult.IsRight) return false;

        var result = store.GetByOriginalValueHashAsync(hash.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var option = (Option<TokenMapping>)result;
        return option.Match(
            Some: m => m.OriginalValueHash == hash.Get,
            None: () => false);
    }

    /// <summary>
    /// Invariant: Getting a non-existent token always returns None (never an error).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetByToken_NonExistent_AlwaysReturnsNone(NonEmptyString token)
    {
        var store = new InMemoryTokenMappingStore();

        var result = store.GetByTokenAsync(token.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var option = (Option<TokenMapping>)result;
        return option.IsNone;
    }

    /// <summary>
    /// Invariant: All stored entries can be independently retrieved by their respective tokens.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Store_MultipleEntries_AllRetrievable()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            count =>
            {
                var store = new InMemoryTokenMappingStore();
                var tokens = new List<string>();

                for (var i = 0; i < count; i++)
                {
                    var uniqueToken = $"token-{Guid.NewGuid():N}";
                    var uniqueHash = $"hash-{Guid.NewGuid():N}";
                    tokens.Add(uniqueToken);

                    var mapping = TokenMapping.Create(
                        token: uniqueToken,
                        originalValueHash: uniqueHash,
                        encryptedOriginalValue: new byte[] { (byte)i },
                        keyId: "key-1");

                    store.StoreAsync(mapping).AsTask().Result
                        .IsRight.ShouldBeTrue();
                }

                store.Count.ShouldBe(count);

                foreach (var tok in tokens)
                {
                    var result = store.GetByTokenAsync(tok).AsTask().Result;
                    result.IsRight.ShouldBeTrue();
                    var option = (Option<TokenMapping>)result;
                    option.IsSome.ShouldBeTrue();
                }
            });
    }

    #endregion

    #region Delete Invariants

    /// <summary>
    /// Invariant: DeleteByKeyIdAsync removes only mappings matching the specified keyId,
    /// leaving mappings with other keyIds intact.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DeleteByKeyId_RemovesOnlyMatchingEntries(
        NonEmptyString keyToDelete,
        NonEmptyString keyToKeep)
    {
        // Skip when both keys are identical (no selectivity to test)
        if (keyToDelete.Get == keyToKeep.Get) return true;

        var store = new InMemoryTokenMappingStore();

        var deleteMapping = TokenMapping.Create(
            token: $"delete-{Guid.NewGuid():N}",
            originalValueHash: $"hash-delete-{Guid.NewGuid():N}",
            encryptedOriginalValue: new byte[] { 1 },
            keyId: keyToDelete.Get);

        var keepMapping = TokenMapping.Create(
            token: $"keep-{Guid.NewGuid():N}",
            originalValueHash: $"hash-keep-{Guid.NewGuid():N}",
            encryptedOriginalValue: new byte[] { 2 },
            keyId: keyToKeep.Get);

        var storeDelete = store.StoreAsync(deleteMapping).AsTask().Result;
        var storeKeep = store.StoreAsync(keepMapping).AsTask().Result;
        if (!storeDelete.IsRight || !storeKeep.IsRight) return false;

        if (store.Count != 2) return false;

        var deleteResult = store.DeleteByKeyIdAsync(keyToDelete.Get).AsTask().Result;
        if (!deleteResult.IsRight) return false;

        // The mapping with keyToDelete should be gone
        var deletedResult = store.GetByTokenAsync(deleteMapping.Token).AsTask().Result;
        if (!deletedResult.IsRight) return false;
        var deletedOption = (Option<TokenMapping>)deletedResult;
        if (deletedOption.IsSome) return false;

        // The mapping with keyToKeep should still exist
        var keptResult = store.GetByTokenAsync(keepMapping.Token).AsTask().Result;
        if (!keptResult.IsRight) return false;
        var keptOption = (Option<TokenMapping>)keptResult;
        return keptOption.IsSome;
    }

    /// <summary>
    /// Invariant: Storing a mapping with a duplicate token overwrites the previous entry
    /// (ConcurrentDictionary indexer behavior).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Store_DuplicateToken_OverwritesPrevious(
        NonEmptyString token,
        NonEmptyString keyId)
    {
        var store = new InMemoryTokenMappingStore();

        var firstMapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: $"hash-first-{Guid.NewGuid():N}",
            encryptedOriginalValue: new byte[] { 1 },
            keyId: keyId.Get);

        var secondMapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: $"hash-second-{Guid.NewGuid():N}",
            encryptedOriginalValue: new byte[] { 2 },
            keyId: keyId.Get);

        var first = store.StoreAsync(firstMapping).AsTask().Result;
        var second = store.StoreAsync(secondMapping).AsTask().Result;
        if (!first.IsRight || !second.IsRight) return false;

        // Only 1 entry for this token (overwritten, not duplicated)
        if (store.Count != 1) return false;

        var result = store.GetByTokenAsync(token.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var option = (Option<TokenMapping>)result;
        return option.Match(
            Some: m => m.Id == secondMapping.Id,
            None: () => false);
    }

    #endregion
}
