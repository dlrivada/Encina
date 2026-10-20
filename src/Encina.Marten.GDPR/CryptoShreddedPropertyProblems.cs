namespace Encina.Marten.GDPR;

/// <summary>
/// The reasons a <see cref="CryptoShreddedAttribute"/> property, or a type that holds one, cannot be
/// encrypted safely. Several reasons can apply to one property at once.
/// </summary>
/// <remarks>
/// <para>
/// Every reason names a shape that would either store personal data in plaintext or make the stored
/// data unreadable. The startup validator and the System.Text.Json contract modifier reject such a shape
/// with a <see cref="CryptoShreddingConfigurationException"/> before any byte is written.
/// </para>
/// <para>
/// <see cref="CryptoShreddedPropertyIssue.Describe"/> turns each flag into one sentence that says how to fix it.
/// </para>
/// </remarks>
[Flags]
public enum CryptoShreddedPropertyProblems
{
    /// <summary>No problem.</summary>
    None = 0,

    /// <summary>The property is not a <see cref="string"/> (string collections are not supported).</summary>
    NotString = 1 << 0,

    /// <summary>The property lacks the companion <c>[PersonalData]</c> attribute.</summary>
    MissingPersonalData = 1 << 1,

    /// <summary>The property has no getter.</summary>
    NotReadable = 1 << 2,

    /// <summary>The property is an indexer.</summary>
    Indexer = 1 << 3,

    /// <summary>The property has no setter or <c>init</c> accessor, so the plaintext cannot be written back after decryption.</summary>
    NoSetter = 1 << 4,

    /// <summary>The property is declared on a struct or record struct, which the deserializer only sees as a boxed copy.</summary>
    DeclaredOnValueType = 1 << 5,

    /// <summary>The attribute is declared on an interface member.</summary>
    DeclaredOnInterface = 1 << 6,

    /// <summary>An interface member carries the attribute but the implementing property does not.</summary>
    AttributeOnlyOnInterface = 1 << 7,

    /// <summary>
    /// The attributed property implements an interface member or overrides a base declaration that has no
    /// attribute, so a member typed as that interface or base is written through the unattributed contract.
    /// </summary>
    ImplementsUnattributedMember = 1 << 8,

    /// <summary>A derived type hides the attributed property with a <c>new</c> property that has no attribute.</summary>
    HiddenByDerivedProperty = 1 << 9,

    /// <summary>
    /// A constructor other than the compiler-synthesized primary constructor of a positional record receives
    /// the value, so it would receive the ciphertext token on read.
    /// </summary>
    BoundToConstructorParameter = 1 << 10,

    /// <summary>The owner implements <c>IJsonOnDeserialized</c>, whose callback would see the ciphertext token.</summary>
    OwnerImplementsOnDeserialized = 1 << 11,

    /// <summary>An owner with value equality is held in a hashed or sorted set, whose order breaks after in-place decryption.</summary>
    OwnerInHashedCollection = 1 << 12,

    /// <summary>The owner type is written by a converter or as a collection, not as a JSON object.</summary>
    OwnerNotSerializedAsObject = 1 << 13,

    /// <summary>A converter writes a type whose member graph reaches an owner, so it could write the child in plaintext.</summary>
    ConverterOverCryptoGraph = 1 << 14,

    /// <summary>A source-generated <c>JsonSerializerContext</c> produced the contract, so the modifier cannot encrypt it.</summary>
    SourceGeneratedContract = 1 << 15,

    /// <summary>The property is not serialized (non-public without <c>[JsonInclude]</c>, or <c>[JsonIgnore]</c>).</summary>
    NotSerialized = 1 << 16,

    /// <summary>The property is serialized but cannot be deserialized (no accessible setter and no constructor parameter).</summary>
    NotDeserializable = 1 << 17,

    /// <summary>The property carries its own <c>[JsonConverter]</c>.</summary>
    CustomConverterOnProperty = 1 << 18,

    /// <summary>A serialized member of the owner cannot be read back (a computed getter beside the encrypted property).</summary>
    ComputedMemberBesideCryptoShredded = 1 << 19,

    /// <summary>A serialized member of a type whose graph reaches an owner cannot be read back (a computed getter).</summary>
    ComputedMemberOverCryptoGraph = 1 << 20,

    /// <summary>An owner type is reachable from a dictionary key type.</summary>
    DictionaryKeyCarriesCryptoShredded = 1 << 21,

    /// <summary>The <see cref="CryptoShreddedAttribute.SubjectIdProperty"/> sibling does not exist on the declaring type or its bases.</summary>
    SubjectIdPropertyNotFound = 1 << 22,

    /// <summary>The subject-id sibling has no getter.</summary>
    SubjectIdPropertyNotReadable = 1 << 23,

    /// <summary>The subject-id sibling is not of a supported subject-id type.</summary>
    SubjectIdTypeUnsupported = 1 << 24,

    /// <summary>The subject-id sibling is not serialized or cannot be deserialized, so decryption could not find the subject.</summary>
    SubjectIdPropertyNotRoundTripped = 1 << 25,

    /// <summary>The subject-id sibling itself carries <see cref="CryptoShreddedAttribute"/>.</summary>
    SubjectIdPropertyIsCryptoShredded = 1 << 26,
}
