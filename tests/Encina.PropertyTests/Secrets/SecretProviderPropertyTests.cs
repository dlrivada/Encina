#pragma warning disable CA2012 // ValueTask should not be awaited multiple times - used via .AsTask().Result in sync property tests

using Encina.Secrets;
using Encina.TestInfrastructure.Extensions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Secrets;

/// <summary>
/// Property-based tests for <see cref="ISecretProvider"/> invariants.
/// Uses <see cref="InMemorySecretProvider"/> as a reference implementation to verify
/// behavioral properties that must hold for all valid implementations.
/// </summary>
public sealed class SecretProviderPropertyTests
{
    // -- Set/Get roundtrip invariants --

    [Property(MaxTest = 50)]
    public bool SetThenGet_ReturnsSetValue(NonEmptyString name, NonEmptyString value)
    {
        var provider = new InMemorySecretProvider();

        var setResult = provider.SetSecretAsync(name.Get, value.Get).AsTask().Result;
        if (!setResult.IsRight) return false;

        var getResult = provider.GetSecretAsync(name.Get).AsTask().Result;
        if (!getResult.IsRight) return false;

        var secret = (Secret)getResult;
        return secret.Value == value.Get && secret.Name == name.Get;
    }

    [Property(MaxTest = 50)]
    public bool DeleteThenGet_ReturnsNotFound(NonEmptyString name, NonEmptyString value)
    {
        var provider = new InMemorySecretProvider();

        // First set so there is something to delete
        var setResult = provider.SetSecretAsync(name.Get, value.Get).AsTask().Result;
        if (!setResult.IsRight) return false;

        var deleteResult = provider.DeleteSecretAsync(name.Get).AsTask().Result;
        if (!deleteResult.IsRight) return false;

        var getResult = provider.GetSecretAsync(name.Get).AsTask().Result;
        if (!getResult.IsLeft) return false;

        var error = (EncinaError)getResult;
        var code = error.GetCode().IfNone(string.Empty);
        return code == SecretsErrorCodes.NotFoundCode;
    }

    [Property(MaxTest = 30)]
    public bool CachedProvider_CacheHit_ReturnsSameValue(NonEmptyString name, NonEmptyString value)
    {
        var inner = new InMemorySecretProvider();
        inner.SetSecretAsync(name.Get, value.Get).AsTask().GetAwaiter().GetResult();

        var cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var cacheOptions = Options.Create(new SecretCacheOptions { Enabled = true, DefaultTtl = TimeSpan.FromMinutes(5) });
        var cached = new CachedSecretProvider(inner, cache, cacheOptions, NullLogger<CachedSecretProvider>.Instance);

        // First call populates cache
        var firstResult = cached.GetSecretAsync(name.Get).AsTask().Result;
        if (!firstResult.IsRight) return false;

        // Remove from inner to confirm subsequent call uses cache
        inner.DeleteSecretAsync(name.Get).AsTask().GetAwaiter().GetResult();

        // Second call should hit cache and return the same value
        var secondResult = cached.GetSecretAsync(name.Get).AsTask().Result;
        if (!secondResult.IsRight) return false;

        var first = (Secret)firstResult;
        var second = (Secret)secondResult;
        return second.Value == first.Value;
    }

    [Property(MaxTest = 50)]
    public bool GetSecret_NeverReturnsNull_InRightCase(NonEmptyString name, NonEmptyString value)
    {
        var provider = new InMemorySecretProvider();
        provider.SetSecretAsync(name.Get, value.Get).AsTask().GetAwaiter().GetResult();

        var result = provider.GetSecretAsync(name.Get).AsTask().Result;
        if (!result.IsRight) return true; // Left results are out of scope for this property

        var secret = (Secret)result;
        // The Right case must never contain null values for the core fields
        return secret is not null
            && secret.Name is not null
            && secret.Value is not null;
    }

    [Property(MaxTest = 50)]
    public bool SetSecret_AlwaysReturnsMetadata_WithName(NonEmptyString name, NonEmptyString value)
    {
        var provider = new InMemorySecretProvider();

        var result = provider.SetSecretAsync(name.Get, value.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var metadata = (SecretMetadata)result;
        return metadata.Name == name.Get
            && metadata.Version is not null;
    }

    [Property(MaxTest = 50)]
    public bool ExistsAfterSet_ReturnsTrue(NonEmptyString name, NonEmptyString value)
    {
        var provider = new InMemorySecretProvider();

        var setResult = provider.SetSecretAsync(name.Get, value.Get).AsTask().Result;
        if (!setResult.IsRight) return false;

        var existsResult = provider.ExistsAsync(name.Get).AsTask().Result;
        if (!existsResult.IsRight) return false;

        return (bool)existsResult;
    }

    [Property(MaxTest = 20)]
    public Property ListAfterMultipleSets_ContainsAllNames()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 8)),
            count =>
            {
                var provider = new InMemorySecretProvider();
                var names = Enumerable.Range(1, count)
                    .Select(i => $"secret-{i}-{Guid.NewGuid():N}")
                    .ToList();

                foreach (var n in names)
                {
                    var setResult = provider.SetSecretAsync(n, "value-for-" + n).AsTask().Result;
                    setResult.IsRight.ShouldBeTrue($"SetSecretAsync should succeed for '{n}'");
                }

                var listResult = provider.ListSecretsAsync().AsTask().Result;
                listResult.IsRight.ShouldBeTrue("ListSecretsAsync should return Right");

                var listed = listResult.Match(
                    Right: names => names,
                    Left: _ => Enumerable.Empty<string>());
                var listedSet = listed.ToHashSet(StringComparer.Ordinal);

                foreach (var n in names)
                {
                    listedSet.ShouldContain(n, $"Listed secrets should contain '{n}' after it was set");
                }
            });
    }

    [Property(MaxTest = 50)]
    public bool ErrorCodes_AlwaysHaveCorrectPrefix(NonEmptyString name)
    {
        // Verify every factory on SecretsErrorCodes produces errors starting with "encina.secrets."
        const string prefix = "encina.secrets.";

        var notFound = SecretsErrorCodes.NotFound(name.Get);
        var accessDenied = SecretsErrorCodes.AccessDenied(name.Get);
        var invalidName = SecretsErrorCodes.InvalidName(name.Get, "test reason");
        var providerUnavailable = SecretsErrorCodes.ProviderUnavailable(name.Get);
        var versionNotFound = SecretsErrorCodes.VersionNotFound(name.Get, "v1");
        var operationFailed = SecretsErrorCodes.OperationFailed("get", "test failure");

        var allErrors = new[] { notFound, accessDenied, invalidName, providerUnavailable, versionNotFound, operationFailed };

        return allErrors.All(e =>
        {
            var code = e.GetCode().IfNone(string.Empty);
            return code.StartsWith(prefix, StringComparison.Ordinal);
        });
    }
}

/// <summary>
/// A simple in-memory implementation of <see cref="ISecretProvider"/> for property tests.
/// Stores secrets in a dictionary. Not thread-safe - intended for single-threaded property tests only.
/// </summary>
internal sealed class InMemorySecretProvider : ISecretProvider
{
    private readonly Dictionary<string, (string Value, string Version, DateTime CreatedAt, DateTime? ExpiresAt)> _store
        = new(StringComparer.Ordinal);

    public ValueTask<Either<EncinaError, Secret>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(name, out var entry))
        {
            return ValueTask.FromResult<Either<EncinaError, Secret>>(
                Either<EncinaError, Secret>.Left(SecretsErrorCodes.NotFound(name)));
        }

        var secret = new Secret(name, entry.Value, entry.Version, entry.ExpiresAt);
        return ValueTask.FromResult<Either<EncinaError, Secret>>(
            Either<EncinaError, Secret>.Right(secret));
    }

    public ValueTask<Either<EncinaError, Secret>> GetSecretVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(name, out var entry) || entry.Version != version)
        {
            return ValueTask.FromResult<Either<EncinaError, Secret>>(
                Either<EncinaError, Secret>.Left(SecretsErrorCodes.VersionNotFound(name, version)));
        }

        var secret = new Secret(name, entry.Value, entry.Version, entry.ExpiresAt);
        return ValueTask.FromResult<Either<EncinaError, Secret>>(
            Either<EncinaError, Secret>.Right(secret));
    }

    public ValueTask<Either<EncinaError, SecretMetadata>> SetSecretAsync(string name, string value, SecretOptions? options = null, CancellationToken cancellationToken = default)
    {
        var version = Guid.NewGuid().ToString("N");
        var createdAt = DateTime.UtcNow;
        _store[name] = (value, version, createdAt, options?.ExpiresAtUtc);

        var metadata = new SecretMetadata(name, version, createdAt, options?.ExpiresAtUtc);
        return ValueTask.FromResult<Either<EncinaError, SecretMetadata>>(
            Either<EncinaError, SecretMetadata>.Right(metadata));
    }

    public ValueTask<Either<EncinaError, Unit>> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_store.Remove(name))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Either<EncinaError, Unit>.Left(SecretsErrorCodes.NotFound(name)));
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(
            Either<EncinaError, Unit>.Right(Unit.Default));
    }

    public ValueTask<Either<EncinaError, IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<string> names = _store.Keys.ToList();
        return ValueTask.FromResult<Either<EncinaError, IEnumerable<string>>>(
            Either<EncinaError, IEnumerable<string>>.Right(names));
    }

    public ValueTask<Either<EncinaError, bool>> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<Either<EncinaError, bool>>(
            Either<EncinaError, bool>.Right(_store.ContainsKey(name)));
    }
}
