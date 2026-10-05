using Encina.Compliance.DataSubjectRights;

namespace Encina.Marten.GDPR;

/// <summary>
/// A nested owner the installation check serializes through every captured <c>JsonSerializerOptions</c>: its
/// output must hold a <c>cs2</c> token and never <see cref="Plaintext"/>.
/// </summary>
internal sealed class CryptoShreddingCanary
{
    /// <summary>The subject the canary frame pre-seeds with an ephemeral key.</summary>
    internal const string Subject = "encina-crypto-shredding-canary";

    /// <summary>The canary plaintext.</summary>
    internal const string Plaintext = "encina-crypto-shredding-canary-plaintext";

    /// <summary>Gets or sets the canary subject id.</summary>
    public string SubjectId { get; set; } = Subject;

    /// <summary>Gets or sets the canary value.</summary>
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
    public string? Value { get; set; } = Plaintext;
}

/// <summary>The holder that makes the canary nested.</summary>
internal sealed class CryptoShreddingCanaryHolder
{
    /// <summary>Gets or sets the nested canary.</summary>
    public CryptoShreddingCanary Inner { get; set; } = new();
}
