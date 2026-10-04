using System.Buffers;
using System.Text;
using System.Text.Json;

using Encina.Marten.GDPR;

using Npgsql;

namespace Encina.UnitTests.Marten.GDPR.Nested;

/// <summary>
/// Every "Encrypted" row of the plan's Shape coverage table: the JSON holds a <c>cs2</c> token and no plaintext,
/// the caller's object is never mutated, and the read returns the plaintext (#1698).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShredderSerializerNestedRoundTripTests : IDisposable
{
    private const string Subject = "subject-a";
    private const string OtherSubject = "subject-b";
    private const string Secret = "secret-mail@example.com";
    private const string OtherSecret = "other-mail@example.com";

    private readonly CryptoHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private static ContactInfo Contact(string subject = Subject, string? email = Secret) => new() { SubjectId = subject, Email = email };

    private string AssertEncrypted<T>(T value, params string[] secrets)
    {
        var json = _harness.Serializer.ToJson(value);
        json.ShouldContain(CryptoShreddingToken.Prefix);
        foreach (var secret in secrets)
        {
            json.ShouldNotContain(secret);
        }

        return json;
    }

    [Fact]
    public void TopLevel_EncryptsAndDecrypts()
    {
        var value = new TopLevelOwner { PatientId = Subject, Email = Secret };

        var json = AssertEncrypted(value, Secret);

        value.Email.ShouldBe(Secret);
        _harness.FromJson<TopLevelOwner>(json).Email.ShouldBe(Secret);
    }

    [Fact]
    public void NestedObject_EncryptsAndDecrypts()
    {
        var value = new NestedEvent { Name = "n", Contact = Contact() };

        var json = AssertEncrypted(value, Secret);

        value.Contact!.Email.ShouldBe(Secret);
        _harness.FromJson<NestedEvent>(json).Contact!.Email.ShouldBe(Secret);
    }

    [Fact]
    public void NullNestedObject_IsWrittenAsNullWithoutAKeyLookup()
    {
        var json = _harness.Serializer.ToJson(new NestedEvent { Name = "n" });

        json.ShouldNotContain(CryptoShreddingToken.Prefix);
        _harness.FromJson<NestedEvent>(json).Contact.ShouldBeNull();
    }

    [Fact]
    public void DeepNesting_EncryptsAndDecrypts()
    {
        var value = new DeepEvent { Level1 = new DeepLevel1 { Level2 = new DeepLevel2 { Contact = Contact() } } };

        var json = AssertEncrypted(value, Secret);

        _harness.FromJson<DeepEvent>(json).Level1!.Level2!.Contact!.Email.ShouldBe(Secret);
    }

    [Fact]
    public void CollectionElements_EncryptAndDecrypt_NullElementsSkipped()
    {
        var value = new CollectionsEvent
        {
            List = [Contact(), null],
            Array = [Contact()],
            ReadOnlyList = [Contact()],
            Enumerable = [Contact()],
            ImmutableArray = [Contact()],
            ImmutableList = [Contact()],
            ReferenceSet = [Contact()],
        };

        var json = AssertEncrypted(value, Secret);

        var read = _harness.FromJson<CollectionsEvent>(json);
        read.List[0]!.Email.ShouldBe(Secret);
        read.List[1].ShouldBeNull();
        read.Array[0].Email.ShouldBe(Secret);
        read.ReadOnlyList[0].Email.ShouldBe(Secret);
        read.Enumerable.Single().Email.ShouldBe(Secret);
        read.ImmutableArray[0].Email.ShouldBe(Secret);
        read.ImmutableList[0].Email.ShouldBe(Secret);
        read.ReferenceSet.Single().Email.ShouldBe(Secret);
    }

    [Fact]
    public void DictionaryValues_EncryptAndDecrypt_KeysUntouched()
    {
        var value = new DictionaryEvent
        {
            Notes = new Dictionary<string, ContactInfo> { ["visible-key"] = Contact() },
            ReadOnlyNotes = new Dictionary<string, ContactInfo> { ["other-key"] = Contact() },
        };

        var json = AssertEncrypted(value, Secret);

        json.ShouldContain("visible-key");
        var read = _harness.FromJson<DictionaryEvent>(json);
        read.Notes["visible-key"].Email.ShouldBe(Secret);
        read.ReadOnlyNotes["other-key"].Email.ShouldBe(Secret);
    }

    [Fact]
    public void ObjectTypedMember_IsEncryptedAndReadsBackAsATokenNeverPlaintext()
    {
        var json = AssertEncrypted(new ObjectMemberEvent { Payload = Contact() }, Secret);

        var read = _harness.FromJson<ObjectMemberEvent>(json);
        read.Payload.ShouldBeOfType<JsonElement>().ToString().ShouldNotContain(Secret);
    }

    [Fact]
    public void PolymorphicMember_EncryptsAndDecrypts()
    {
        var value = new PolymorphicEvent { Note = new ContactNote { Title = "t", SubjectId = Subject, Text = Secret } };

        var json = AssertEncrypted(value, Secret);

        _harness.FromJson<PolymorphicEvent>(json).Note.ShouldBeOfType<ContactNote>().Text.ShouldBe(Secret);
    }

    [Fact]
    public void PositionalRecord_EncryptsAndDecryptsThroughTheInitSetter()
    {
        var json = AssertEncrypted(new PositionalRecordOwner(Subject, Secret), Secret);

        _harness.FromJson<PositionalRecordOwner>(json).Email.ShouldBe(Secret);
    }

    [Fact]
    public void WidePositionalRecord_EncryptsAndDecrypts()
    {
        var json = AssertEncrypted(new WidePositionalRecordOwner("a", "b", "c", Subject, Secret), Secret);

        _harness.FromJson<WidePositionalRecordOwner>(json).Email.ShouldBe(Secret);
    }

    [Fact]
    public void InitProperty_EncryptsAndDecrypts()
    {
        var json = AssertEncrypted(new InitOwner { PatientId = Subject, Email = Secret }, Secret);

        _harness.FromJson<InitOwner>(json).Email.ShouldBe(Secret);
    }

    [Fact]
    public void PrivateSetterWithJsonInclude_EncryptsAndDecrypts()
    {
        var value = new PrivateSetterOwner { PatientId = Subject };
        value.SetEmail(Secret);

        var json = AssertEncrypted(value, Secret);

        _harness.FromJson<PrivateSetterOwner>(json).Email.ShouldBe(Secret);
    }

    [Fact]
    public void NonPublicPropertyWithJsonInclude_EncryptsAndDecrypts()
    {
        var json = AssertEncrypted(new NonPublicOwner { PatientId = Subject, Secret = Secret }, Secret);

        _harness.FromJson<NonPublicOwner>(json).Secret.ShouldBe(Secret);
    }

    [Fact]
    public void PrivatePropertyDeclaredOnABaseClass_EncryptsAndDecrypts()
    {
        var value = new NonPublicDerived { PatientId = Subject };
        value.WriteHidden(Secret);

        var json = AssertEncrypted(value, Secret);

        _harness.FromJson<NonPublicDerived>(json).ReadHidden().ShouldBe(Secret);
    }

    [Fact]
    public void ClosedGenericSubjectId_EncryptsAndDecrypts()
    {
        var id = Guid.NewGuid();
        var value = new GenericHolder { Item = new GenericOwner<Guid> { Id = id, Note = Secret } };

        var json = AssertEncrypted(value, Secret);

        _harness.FromJson<GenericHolder>(json).Item!.Note.ShouldBe(Secret);
    }

    [Fact]
    public void DifferentSubjectPerLevel_UsesEachSubjectsKey()
    {
        var value = new TwoSubjectEvent { PatientId = Subject, PatientEmail = Secret, Therapist = Contact(OtherSubject, OtherSecret) };

        var json = AssertEncrypted(value, Secret, OtherSecret);

        var read = _harness.FromJson<TwoSubjectEvent>(json);
        read.PatientEmail.ShouldBe(Secret);
        read.Therapist!.Email.ShouldBe(OtherSecret);
    }

    [Fact]
    public void SharedInstance_IsEncryptedPerOccurrenceWithFreshNonces()
    {
        var shared = Contact();
        var json = AssertEncrypted(new SharedReferenceEvent { First = shared, Second = shared }, Secret);

        using var document = JsonDocument.Parse(json);
        var first = document.RootElement.GetProperty("First").GetProperty("Email").GetString();
        var second = document.RootElement.GetProperty("Second").GetProperty("Email").GetString();
        first.ShouldNotBe(second);
        var read = _harness.FromJson<SharedReferenceEvent>(json);
        read.First!.Email.ShouldBe(Secret);
        read.Second!.Email.ShouldBe(Secret);
    }

    [Fact]
    public void VirtualOverride_IsEncryptedOnce()
    {
        var json = AssertEncrypted(new VirtualOverrideOwner { PatientId = Subject, Email = Secret }, Secret);

        json.Split(CryptoShreddingToken.Prefix).Length.ShouldBe(2);
        _harness.FromJson<VirtualOverrideOwner>(json).Email.ShouldBe(Secret);
    }

    [Fact]
    public void GuidSubjectId_EncryptsAndDecrypts()
    {
        var json = AssertEncrypted(new SubjectTypedOwner { PatientId = Guid.NewGuid(), Email = Secret }, Secret);

        _harness.FromJson<SubjectTypedOwner>(json).Email.ShouldBe(Secret);
    }

    [Fact]
    public void NullValue_StaysNullWithoutAKeyLookup()
    {
        var json = _harness.Serializer.ToJson(new TopLevelOwner { PatientId = null, Email = null });

        json.ShouldNotContain(CryptoShreddingToken.Prefix);
        _harness.FromJson<TopLevelOwner>(json).Email.ShouldBeNull();
    }

    [Fact]
    public void DocumentWithoutPii_IsByteIdenticalToTheInnerSerializer()
    {
        var value = new NoPiiEvent { Name = "x", Count = 3 };

        _harness.Serializer.ToJson(value).ShouldBe(_harness.Inner.ToJson(value));
        _harness.Serializer.ToCleanJson(value).ShouldBe(_harness.Inner.ToCleanJson(value));
    }

    public static TheoryData<string> WriteMembers => new()
    {
        "ToJson", "ToCleanJson", "ToJsonWithTypes", "WriteTo", "WriteToCleanJson", "WriteToJsonWithTypes", "WriteToParameter", "WriteToNpgsqlParameter"
    };

    [Theory]
    [MemberData(nameof(WriteMembers))]
    public void EveryWriteMember_EncryptsNestedData(string member)
    {
        var value = new NestedEvent { Name = "n", Contact = Contact() };

        var output = Write(member, value);

        output.ShouldContain(CryptoShreddingToken.Prefix);
        output.ShouldNotContain(Secret);
        value.Contact!.Email.ShouldBe(Secret);
    }

    [Fact]
    public async Task EveryReadMember_DecryptsNestedData()
    {
        var json = _harness.Serializer.ToJson(new NestedEvent { Contact = Contact() });
        var bytes = Encoding.UTF8.GetBytes(json);

        _harness.Serializer.FromJson<NestedEvent>(new MemoryStream(bytes)).Contact!.Email.ShouldBe(Secret);
        ((NestedEvent)_harness.Serializer.FromJson(typeof(NestedEvent), new MemoryStream(bytes))).Contact!.Email.ShouldBe(Secret);
        (await _harness.Serializer.FromJsonAsync<NestedEvent>(new MemoryStream(bytes))).Contact!.Email.ShouldBe(Secret);
        ((NestedEvent)await _harness.Serializer.FromJsonAsync(typeof(NestedEvent), new MemoryStream(bytes))).Contact!.Email.ShouldBe(Secret);
    }

    [Fact]
    public void Delegates_EnumStorageCasingAndValueCasting()
    {
        _harness.Serializer.EnumStorage.ShouldBe(_harness.Inner.EnumStorage);
        _harness.Serializer.Casing.ShouldBe(_harness.Inner.Casing);
        _harness.Serializer.ValueCasting.ShouldBe(_harness.Inner.ValueCasting);
    }

    private string Write(string member, object value)
    {
        var serializer = _harness.Serializer;
        var buffer = new ArrayBufferWriter<byte>();
        switch (member)
        {
            case "ToJson": return serializer.ToJson(value);
            case "ToCleanJson": return serializer.ToCleanJson(value);
            case "ToJsonWithTypes": return serializer.ToJsonWithTypes(value);
            case "WriteTo": serializer.WriteTo(buffer, value); break;
            case "WriteToCleanJson": serializer.WriteToCleanJson(buffer, value); break;
            case "WriteToJsonWithTypes": serializer.WriteToJsonWithTypes(buffer, value); break;
            case "WriteToParameter":
                var parameter = new NpgsqlParameter();
                serializer.WriteToParameter((System.Data.Common.DbParameter)parameter, value);
                return ParameterText(parameter);
            default:
                var npgsql = new NpgsqlParameter();
                serializer.WriteToParameter(npgsql, value);
                return ParameterText(npgsql);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string ParameterText(NpgsqlParameter parameter) => parameter.Value switch
    {
        string text => text,
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
        ArraySegment<byte> segment => Encoding.UTF8.GetString(segment),
        null => "<null>",
        var other => "<" + other.GetType().FullName + ">",
    };
}
