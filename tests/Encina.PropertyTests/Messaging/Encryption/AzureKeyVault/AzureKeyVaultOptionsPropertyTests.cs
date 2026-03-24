using Encina.Messaging.Encryption.AzureKeyVault;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Property-based tests for <see cref="AzureKeyVaultOptions"/> invariants.
/// Verifies property round-trips and ToString safety.
/// </summary>
[Trait("Category", "Property")]
public sealed class AzureKeyVaultOptionsPropertyTests
{
    #region KeyName Round-Trip

    /// <summary>
    /// Property: KeyName round-trip for any non-null string (set then get returns same value).
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_KeyName_SetGetRoundTrip()
    {
        return Prop.ForAll(
            Arb.From(GenKeyName()),
            keyName =>
            {
                var options = new AzureKeyVaultOptions { KeyName = keyName };

                return options.KeyName == keyName;
            });
    }

    /// <summary>
    /// Property: KeyName is independent of VaultUri (setting both preserves each value).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_KeyName_IsIndependentOfVaultUri()
    {
        return Prop.ForAll(
            Arb.From(GenKeyName()),
            Arb.From(GenVaultUri()),
            (keyName, vaultUri) =>
            {
                var options = new AzureKeyVaultOptions
                {
                    KeyName = keyName,
                    VaultUri = vaultUri
                };

                return options.KeyName == keyName
                       && options.VaultUri == vaultUri;
            });
    }

    #endregion

    #region VaultUri Round-Trip

    /// <summary>
    /// Property: VaultUri round-trip (set then get returns same value).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_VaultUri_SetGetRoundTrip()
    {
        return Prop.ForAll(
            Arb.From(GenVaultUri()),
            vaultUri =>
            {
                var options = new AzureKeyVaultOptions { VaultUri = vaultUri };

                return options.VaultUri == vaultUri;
            });
    }

    #endregion

    #region KeyVersion Round-Trip

    /// <summary>
    /// Property: KeyVersion round-trip for any non-null string.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_KeyVersion_SetGetRoundTrip()
    {
        return Prop.ForAll(
            Arb.From(GenKeyVersion()),
            version =>
            {
                var options = new AzureKeyVaultOptions { KeyVersion = version };

                return options.KeyVersion == version;
            });
    }

    #endregion

    #region ToString Safety

    /// <summary>
    /// Property: ToString never throws and produces a non-empty result for any valid configuration.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ToString_NeverThrows()
    {
        return Prop.ForAll(
            Arb.From(GenKeyName()),
            Arb.From(GenVaultUri()),
            (keyName, vaultUri) =>
            {
                var options = new AzureKeyVaultOptions
                {
                    KeyName = keyName,
                    VaultUri = vaultUri
                };

                try
                {
                    var result = options.ToString();
                    return !string.IsNullOrEmpty(result);
                }
                catch
                {
                    return false;
                }
            });
    }

    /// <summary>
    /// Property: ToString output contains the key name.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ToString_ContainsKeyName()
    {
        return Prop.ForAll(
            Arb.From(GenKeyName()),
            keyName =>
            {
                var options = new AzureKeyVaultOptions { KeyName = keyName };
                var str = options.ToString();

                return str!.Contains(keyName, StringComparison.Ordinal);
            });
    }

    #endregion

    #region Default Values

    /// <summary>
    /// Default options have null for optional properties.
    /// </summary>
    [Fact]
    public void DefaultOptions_HaveNullOptionalProperties()
    {
        var options = new AzureKeyVaultOptions();

        options.VaultUri.ShouldBeNull();
        options.KeyName.ShouldBeNull();
        options.KeyVersion.ShouldBeNull();
        options.Credential.ShouldBeNull();
        options.ClientOptions.ShouldBeNull();
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generates key name strings resembling Azure Key Vault key names.
    /// </summary>
    private static Gen<string> GenKeyName()
    {
        return Gen.Elements("my-key", "encryption-key", "msg-enc", "primary", "secondary")
            .SelectMany(prefix =>
                Gen.Choose(1, 9999).Select(n => $"{prefix}-{n}"));
    }

    /// <summary>
    /// Generates Azure Key Vault URI instances.
    /// </summary>
    private static Gen<Uri> GenVaultUri()
    {
        return Gen.Elements("myvault", "testvault", "prodvault", "devvault", "staging")
            .Select(name => new Uri($"https://{name}.vault.azure.net/"));
    }

    /// <summary>
    /// Generates key version strings (hex-like identifiers).
    /// </summary>
    private static Gen<string> GenKeyVersion()
    {
        return Gen.Choose(1, int.MaxValue)
            .Select(n => n.ToString("x8", System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion
}
