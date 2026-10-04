using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;

#pragma warning disable CA1819, CA1002, CA2227, CA1065, S2376, S1144, IDE0051, IDE0052, CA1822, S4144, CA1036, S1210

namespace Encina.UnitTests.Marten.GDPR.Nested;

// ---- Valid shapes (Shape coverage table, "Encrypted" rows) -------------------------------------------------------

public sealed class TopLevelOwner
{
    public string? PatientId { get; set; }

    [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true, Portable = true)]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class ContactInfo
{
    public string? SubjectId { get; set; }

    [PersonalData(Category = PersonalDataCategory.Contact)]
    [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
    public string? Email { get; set; }
}

public sealed class NestedEvent
{
    public string? Name { get; set; }

    public ContactInfo? Contact { get; set; }
}

public sealed class DeepEvent
{
    public DeepLevel1? Level1 { get; set; }
}

public sealed class DeepLevel1
{
    public DeepLevel2? Level2 { get; set; }
}

public sealed class DeepLevel2
{
    public ContactInfo? Contact { get; set; }
}

public sealed class CollectionsEvent
{
    public List<ContactInfo?> List { get; set; } = [];

    public ContactInfo[] Array { get; set; } = [];

    public IReadOnlyList<ContactInfo> ReadOnlyList { get; set; } = [];

    public IEnumerable<ContactInfo> Enumerable { get; set; } = [];

    public ImmutableArray<ContactInfo> ImmutableArray { get; set; } = [];

    public ImmutableList<ContactInfo> ImmutableList { get; set; } = [];

    public System.Collections.Generic.HashSet<ContactInfo> ReferenceSet { get; set; } = [];
}

public sealed class DictionaryEvent
{
    public Dictionary<string, ContactInfo> Notes { get; set; } = [];

    public IReadOnlyDictionary<string, ContactInfo> ReadOnlyNotes { get; set; } = new Dictionary<string, ContactInfo>();
}

public sealed class ObjectMemberEvent
{
    public object? Payload { get; set; }
}

[JsonDerivedType(typeof(ContactNote), "contact")]
public abstract class NoteBase
{
    public string? Title { get; set; }
}

public sealed class ContactNote : NoteBase
{
    public string? SubjectId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
    public string? Text { get; set; }
}

public sealed class PolymorphicEvent
{
    public NoteBase? Note { get; set; }
}

public sealed record PositionalRecordOwner(
    string? PatientId,
    [property: PersonalData]
    [property: CryptoShredded(SubjectIdProperty = "PatientId")]
    string? Email);

public sealed record WidePositionalRecordOwner(
    string? A,
    string? B,
    string? C,
    string? PatientId,
    [property: PersonalData]
    [property: CryptoShredded(SubjectIdProperty = "PatientId")]
    string? Email);

public sealed class InitOwner
{
    public string? PatientId { get; init; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; init; }
}

public sealed class PrivateSetterOwner
{
    public string? PatientId { get; set; }

    [JsonInclude]
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; private set; }

    public void SetEmail(string? email) => Email = email;
}

public sealed class NonPublicOwner
{
    public string? PatientId { get; set; }

    [JsonInclude]
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    internal string? Secret { get; set; }
}

public abstract class NonPublicBase
{
    public string? PatientId { get; set; }

    [JsonInclude]
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    private string? Hidden { get; set; }

    public string? ReadHidden() => Hidden;

    public void WriteHidden(string? value) => Hidden = value;
}

public sealed class NonPublicDerived : NonPublicBase;

public sealed class GenericOwner<TId>
{
    public TId Id { get; set; } = default!;

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(Id))]
    public string? Note { get; set; }
}

public sealed class GenericHolder
{
    public GenericOwner<Guid>? Item { get; set; }
}

public sealed class TwoSubjectEvent
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? PatientEmail { get; set; }

    public ContactInfo? Therapist { get; set; }
}

public sealed class SharedReferenceEvent
{
    public ContactInfo? First { get; set; }

    public ContactInfo? Second { get; set; }
}

public class VirtualBaseOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public virtual string? Email { get; set; }
}

public sealed class VirtualOverrideOwner : VirtualBaseOwner
{
    public override string? Email { get; set; }
}

public sealed class NoPiiEvent
{
    public string? Name { get; set; }

    public int Count { get; set; }
}

public sealed class SubjectTypedOwner
{
    public Guid PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class ObjectSubjectOwner
{
    public object? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

// ---- Misconfigured shapes (Shape coverage table, "Rejected" rows) ------------------------------------------------

public sealed class NotStringOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public int Age { get; set; }
}

public sealed class StringListOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public List<string> Emails { get; set; } = [];
}

public sealed class MissingPersonalDataOwner
{
    public string? PatientId { get; set; }

    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class GetterOnlyOwner(string? patientId, string? email)
{
    public string? PatientId { get; set; } = patientId;

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; } = email;
}

public sealed class SetOnlyOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string Email { set { _ = value; } }
}

public sealed class IndexerOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string this[int index]
    {
        get => string.Empty;
        set { _ = value; }
    }
}

public struct StructOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public interface IAttributedContact
{
    string? SubjectId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
    string? Email { get; set; }
}

public sealed class InterfaceOnlyAttributeOwner : IAttributedContact
{
    public string? SubjectId { get; set; }

    public string? Email { get; set; }
}

public interface IContactPlain
{
    string? Email { get; set; }
}

public sealed class ImplementsUnattributedOwner : IContactPlain
{
    public string? SubjectId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
    public string? Email { get; set; }
}

public abstract class PlainBaseContact
{
    public string? SubjectId { get; set; }

    public abstract string? Email { get; set; }
}

public sealed class OverrideOnlyAttributedOwner : PlainBaseContact
{
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
    public override string? Email { get; set; }
}

public class HiddenBaseOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class HidingDerivedOwner : HiddenBaseOwner
{
    public new string? Email { get; set; }
}

public sealed record TrimmingRecordOwner(string? PatientId, string? Email)
{
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; init; } = Email?.Trim();
}

public sealed class JsonConstructorOwner
{
    [JsonConstructor]
    public JsonConstructorOwner(string? patientId, string? email)
    {
        PatientId = patientId;
        Email = email?.ToLowerInvariant();
    }

    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class OnDeserializedOwner : IJsonOnDeserialized
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }

    public void OnDeserialized()
    {
    }
}

public sealed record RecordContact
{
    public string? SubjectId { get; init; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
    public string? Email { get; init; }
}

public sealed class HashedRecordHolder
{
    public System.Collections.Generic.HashSet<RecordContact> Contacts { get; set; } = [];
}

public sealed class ComparableContact : IComparable<ComparableContact>
{
    public string? SubjectId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
    public string? Email { get; set; }

    public int CompareTo(ComparableContact? other) => string.CompareOrdinal(Email, other?.Email);
}

public sealed class SortedComparableHolder
{
    public SortedSet<ComparableContact> Contacts { get; set; } = [];
}

public sealed class OpaqueConverter<T> : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue("opaque");
}

[JsonConverter(typeof(OpaqueConverter<ConverterOwner>))]
public sealed class ConverterOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

[JsonConverter(typeof(OpaqueConverter<ConverterContainer>))]
public sealed class ConverterContainer
{
    public ContactInfo? Contact { get; set; }
}

public sealed class PropertyConverterContainer
{
    [JsonConverter(typeof(OpaqueConverter<ContactInfo>))]
    public ContactInfo? Contact { get; set; }
}

public sealed class PropertyConverterOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [JsonConverter(typeof(OpaqueConverter<string>))]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class NotSerializedOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    internal string? Email { get; set; }
}

public sealed class IgnoredOwner
{
    public string? PatientId { get; set; }

    [JsonIgnore]
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class NotDeserializableOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; private set; }
}

public sealed class ComputedBesideOwner
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }

    public string? Masked => Email?[..1];
}

public sealed class ComputedContainer
{
    public ContactInfo? Contact { get; set; }

    public string? Summary => Contact?.Email;
}

public sealed class DictionaryKeyContainer
{
    public Dictionary<ContactInfo, string> ByContact { get; set; } = [];
}

public sealed class SubjectNotFoundOwner
{
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = "Missing")]
    public string? Email { get; set; }
}

public sealed class SubjectNotReadableOwner
{
    public string PatientId { set { _ = value; } }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class SubjectUnsupportedOwner
{
    public decimal PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class SubjectNotRoundTrippedOwner
{
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }

    internal string? PatientId { get; set; }
}

public sealed class SubjectIsCryptoOwner
{
    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(Email))]
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class ObjectGenericHolder
{
    public GenericOwner<object>? Item { get; set; }
}

public sealed class PersonalDataSubjectOwner
{
    [PersonalData]
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

#pragma warning restore CA1819, CA1002, CA2227, CA1065, S2376, S1144, IDE0051, IDE0052, CA1822, S4144, CA1036, S1210
