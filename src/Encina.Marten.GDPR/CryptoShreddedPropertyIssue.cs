namespace Encina.Marten.GDPR;

/// <summary>
/// One misconfigured <see cref="CryptoShreddedAttribute"/> property (or a member of a type on the crypto
/// graph) together with every reason it cannot be encrypted safely.
/// </summary>
/// <param name="DeclaringTypeName">The full name of the type the problem is reported on.</param>
/// <param name="PropertyName">The name of the property or member the problem is about.</param>
/// <param name="Problems">Every reason that applies, as flags.</param>
/// <remarks>
/// An issue carries type names, member names and flags only: never a value or a subject id, so it is safe
/// to log and to put in an exception message.
/// </remarks>
public sealed record CryptoShreddedPropertyIssue(
    string DeclaringTypeName,
    string PropertyName,
    CryptoShreddedPropertyProblems Problems)
{
    /// <summary>
    /// Describes the issue as <c>Type.Property: sentence; sentence</c>, one actionable sentence per problem flag.
    /// </summary>
    /// <returns>The description.</returns>
    public string Describe()
    {
        var sentences = Enum.GetValues<CryptoShreddedPropertyProblems>()
            .Where(flag => flag != CryptoShreddedPropertyProblems.None && Problems.HasFlag(flag))
            .Select(DescribeFlag);

        return $"{DeclaringTypeName}.{PropertyName}: {string.Join(" ", sentences)}";
    }

    /// <inheritdoc />
    public override string ToString() => $"{DeclaringTypeName}.{PropertyName}: {Problems}";

    // crap-exempt: single-question switch — problem flag to explanation
    internal static string DescribeFlag(CryptoShreddedPropertyProblems flag) => flag switch
    {
        CryptoShreddedPropertyProblems.NotString => "Only string properties can be crypto-shredded; string collections are not supported.",
        CryptoShreddedPropertyProblems.MissingPersonalData => "Add [PersonalData]; [CryptoShredded] requires it.",
        CryptoShreddedPropertyProblems.NotReadable => "Add a getter.",
        CryptoShreddedPropertyProblems.Indexer => "Indexers cannot be crypto-shredded.",
        CryptoShreddedPropertyProblems.NoSetter => "Add a setter or an init accessor so the plaintext can be written back after decryption.",
        CryptoShreddedPropertyProblems.DeclaredOnValueType => "Declare the owner as a class or a record class, not a struct.",
        CryptoShreddedPropertyProblems.DeclaredOnInterface => "Do not declare personal data on interfaces; type members with the concrete class.",
        CryptoShreddedPropertyProblems.AttributeOnlyOnInterface => "Put [CryptoShredded] on the implementing property, and keep personal data off interfaces.",
        CryptoShreddedPropertyProblems.ImplementsUnattributedMember => "The property implements or overrides an unattributed declaration; type the member with the concrete class or attribute the base declaration.",
        CryptoShreddedPropertyProblems.HiddenByDerivedProperty => "A derived 'new' property without the attribute hides this one and would be written in plaintext; remove the hiding or attribute it.",
        CryptoShreddedPropertyProblems.BoundToConstructorParameter => "A constructor receives this value and would see the ciphertext; use an init property or a positional record.",
        CryptoShreddedPropertyProblems.OwnerImplementsOnDeserialized => "Remove IJsonOnDeserialized from the owner; its callback runs before decryption.",
        CryptoShreddedPropertyProblems.OwnerInHashedCollection => "Hold value-equality owners in a list, not in a hashed or sorted set.",
        CryptoShreddedPropertyProblems.OwnerNotSerializedAsObject => "The owner is written by a converter or as a collection; remove the converter.",
        CryptoShreddedPropertyProblems.ConverterOverCryptoGraph => "A converter writes a type that reaches crypto-shredded data; remove the converter.",
        CryptoShreddedPropertyProblems.SourceGeneratedContract => "A source-generated JsonSerializerContext produced the contract; use the reflection resolver for crypto-shredded types.",
        CryptoShreddedPropertyProblems.NotSerialized => "The property is not serialized; add [JsonInclude] to a non-public property or remove [JsonIgnore].",
        CryptoShreddedPropertyProblems.NotDeserializable => "The property cannot be deserialized; make the setter accessible or add [JsonInclude].",
        CryptoShreddedPropertyProblems.CustomConverterOnProperty => "Remove [JsonConverter] from the crypto-shredded property.",
        CryptoShreddedPropertyProblems.ComputedMemberBesideCryptoShredded => "A computed member is serialized beside crypto-shredded data; add [JsonIgnore] or make it settable.",
        CryptoShreddedPropertyProblems.ComputedMemberOverCryptoGraph => "A computed member is serialized on a type that reaches crypto-shredded data; add [JsonIgnore] or make it settable.",
        CryptoShreddedPropertyProblems.DictionaryKeyCarriesCryptoShredded => "Crypto-shredded data cannot be part of a dictionary key.",
        CryptoShreddedPropertyProblems.SubjectIdPropertyNotFound => "The SubjectIdProperty sibling does not exist on the declaring type or its bases.",
        CryptoShreddedPropertyProblems.SubjectIdPropertyNotReadable => "The SubjectIdProperty sibling has no getter.",
        CryptoShreddedPropertyProblems.SubjectIdTypeUnsupported => "The SubjectIdProperty sibling must be a string, Guid, integer or strongly-typed id.",
        CryptoShreddedPropertyProblems.SubjectIdPropertyNotRoundTripped => "The SubjectIdProperty sibling must be serialized and deserializable.",
        CryptoShreddedPropertyProblems.SubjectIdPropertyIsCryptoShredded => "The SubjectIdProperty sibling cannot itself be [CryptoShredded].",
        _ => flag.ToString(),
    };
}
