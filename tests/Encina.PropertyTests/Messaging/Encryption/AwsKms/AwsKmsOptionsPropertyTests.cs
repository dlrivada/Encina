using Encina.Messaging.Encryption.AwsKms;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Messaging.Encryption.AwsKms;

/// <summary>
/// Property-based tests for <see cref="AwsKmsOptions"/> invariants.
/// Verifies property round-trips and default value semantics.
/// </summary>
[Trait("Category", "Property")]
public sealed class AwsKmsOptionsPropertyTests
{
    #region KeyId Round-Trip

    /// <summary>
    /// Property: KeyId round-trip (set then get returns same value for any non-null string).
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_KeyId_SetGetRoundTrip()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyKeyId()),
            keyId =>
            {
                var options = new AwsKmsOptions { KeyId = keyId };

                return options.KeyId == keyId;
            });
    }

    /// <summary>
    /// Property: KeyId is independent of other properties (setting Region does not affect KeyId).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_KeyId_IsIndependentOfRegion()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyKeyId()),
            Arb.From(GenAwsRegion()),
            (keyId, region) =>
            {
                var options = new AwsKmsOptions
                {
                    KeyId = keyId,
                    Region = region
                };

                return options.KeyId == keyId && options.Region == region;
            });
    }

    #endregion

    #region EncryptionAlgorithm Default

    /// <summary>
    /// Property: EncryptionAlgorithm defaults to "SYMMETRIC_DEFAULT".
    /// </summary>
    [Fact]
    public void DefaultEncryptionAlgorithm_IsSymmetricDefault()
    {
        var options = new AwsKmsOptions();

        options.EncryptionAlgorithm.ShouldBe("SYMMETRIC_DEFAULT");
    }

    /// <summary>
    /// Property: EncryptionAlgorithm round-trip for any non-null string.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_EncryptionAlgorithm_SetGetRoundTrip()
    {
        return Prop.ForAll(
            Arb.From(GenAlgorithmString()),
            algorithm =>
            {
                var options = new AwsKmsOptions { EncryptionAlgorithm = algorithm };

                return options.EncryptionAlgorithm == algorithm;
            });
    }

    #endregion

    #region Region Round-Trip

    /// <summary>
    /// Property: Region round-trip (set then get returns same value for any non-null string).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_Region_SetGetRoundTrip()
    {
        return Prop.ForAll(
            Arb.From(GenAwsRegion()),
            region =>
            {
                var options = new AwsKmsOptions { Region = region };

                return options.Region == region;
            });
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generates non-empty strings resembling AWS KMS key identifiers.
    /// </summary>
    private static Gen<string> GenNonEmptyKeyId()
    {
        return Gen.Elements(
                "arn:aws:kms:us-east-1:123456789012:key/",
                "alias/",
                "mrk-",
                "key-")
            .SelectMany(prefix =>
                Gen.Choose(1, 999999).Select(n => $"{prefix}{n:D6}"));
    }

    /// <summary>
    /// Generates AWS region strings.
    /// </summary>
    private static Gen<string> GenAwsRegion()
    {
        return Gen.Elements(
            "us-east-1", "us-west-2", "eu-west-1", "eu-central-1",
            "ap-southeast-1", "ap-northeast-1", "sa-east-1");
    }

    /// <summary>
    /// Generates encryption algorithm strings.
    /// </summary>
    private static Gen<string> GenAlgorithmString()
    {
        return Gen.Elements(
            "SYMMETRIC_DEFAULT", "RSAES_OAEP_SHA_1", "RSAES_OAEP_SHA_256",
            "SM2PKE");
    }

    #endregion
}
