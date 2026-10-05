using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Encina.Marten.GDPR;

namespace Encina.UnitTests.Marten.GDPR.Nested;

/// <summary>
/// One test per problem flag of the classifier (reflection rules and System.Text.Json contract rules), on the
/// "Rejected" rows of the plan's Shape coverage table (#1698).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShreddedPropertyClassifierTests
{
    private static readonly JsonSerializerOptions Options = new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    private static IReadOnlyList<CryptoShreddedPropertyIssue> Classify(Type type) =>
        CryptoShreddedContractRules.Classify(Options.GetTypeInfo(type), CryptoShreddedPropertyClassifier.GetShape(type));

    public static TheoryData<Type, string, CryptoShreddedPropertyProblems> Rejected => new()
    {
        { typeof(NotStringOwner), nameof(NotStringOwner.Age), CryptoShreddedPropertyProblems.NotString },
        { typeof(StringListOwner), nameof(StringListOwner.Emails), CryptoShreddedPropertyProblems.NotString },
        { typeof(MissingPersonalDataOwner), "Email", CryptoShreddedPropertyProblems.MissingPersonalData },
        { typeof(GetterOnlyOwner), "Email", CryptoShreddedPropertyProblems.NoSetter },
        { typeof(SetOnlyOwner), "Email", CryptoShreddedPropertyProblems.NotReadable },
        { typeof(IndexerOwner), "Item", CryptoShreddedPropertyProblems.Indexer },
        { typeof(StructOwner), "Email", CryptoShreddedPropertyProblems.DeclaredOnValueType },
        { typeof(IAttributedContact), "Email", CryptoShreddedPropertyProblems.DeclaredOnInterface },
        { typeof(InterfaceOnlyAttributeOwner), "Email", CryptoShreddedPropertyProblems.AttributeOnlyOnInterface },
        { typeof(ImplementsUnattributedOwner), "Email", CryptoShreddedPropertyProblems.ImplementsUnattributedMember },
        { typeof(OverrideOnlyAttributedOwner), "Email", CryptoShreddedPropertyProblems.ImplementsUnattributedMember },
        { typeof(HidingDerivedOwner), "Email", CryptoShreddedPropertyProblems.HiddenByDerivedProperty },
        { typeof(TrimmingRecordOwner), "Email", CryptoShreddedPropertyProblems.BoundToConstructorParameter },
        { typeof(JsonConstructorOwner), "Email", CryptoShreddedPropertyProblems.BoundToConstructorParameter },
        { typeof(OnDeserializedOwner), "Email", CryptoShreddedPropertyProblems.OwnerImplementsOnDeserialized },
        { typeof(HashedRecordHolder), nameof(HashedRecordHolder.Contacts), CryptoShreddedPropertyProblems.OwnerInHashedCollection },
        { typeof(SortedComparableHolder), nameof(SortedComparableHolder.Contacts), CryptoShreddedPropertyProblems.OwnerInHashedCollection },
        { typeof(ConverterOwner), "Email", CryptoShreddedPropertyProblems.OwnerNotSerializedAsObject },
        { typeof(ConverterContainer), "(type)", CryptoShreddedPropertyProblems.ConverterOverCryptoGraph },
        { typeof(PropertyConverterContainer), "Contact", CryptoShreddedPropertyProblems.ConverterOverCryptoGraph },
        { typeof(ConvertedContactList), "(type)", CryptoShreddedPropertyProblems.ConverterOverCryptoGraph },
        { typeof(HashedWrapperHolder), nameof(HashedWrapperHolder.Wrappers), CryptoShreddedPropertyProblems.OwnerInHashedCollection },
        { typeof(PropertyConverterOwner), "Email", CryptoShreddedPropertyProblems.CustomConverterOnProperty },
        { typeof(NotSerializedOwner), "Email", CryptoShreddedPropertyProblems.NotSerialized },
        { typeof(IgnoredOwner), "Email", CryptoShreddedPropertyProblems.NotSerialized },
        { typeof(NotDeserializableOwner), "Email", CryptoShreddedPropertyProblems.NotDeserializable },
        { typeof(ComputedBesideOwner), nameof(ComputedBesideOwner.Masked), CryptoShreddedPropertyProblems.ComputedMemberBesideCryptoShredded },
        { typeof(ComputedContainer), nameof(ComputedContainer.Summary), CryptoShreddedPropertyProblems.ComputedMemberOverCryptoGraph },
        { typeof(DictionaryKeyContainer), nameof(DictionaryKeyContainer.ByContact), CryptoShreddedPropertyProblems.DictionaryKeyCarriesCryptoShredded },
        { typeof(SubjectNotFoundOwner), "Email", CryptoShreddedPropertyProblems.SubjectIdPropertyNotFound },
        { typeof(SubjectNotReadableOwner), "Email", CryptoShreddedPropertyProblems.SubjectIdPropertyNotReadable },
        { typeof(SubjectUnsupportedOwner), "Email", CryptoShreddedPropertyProblems.SubjectIdTypeUnsupported },
        { typeof(SubjectNotRoundTrippedOwner), "Email", CryptoShreddedPropertyProblems.SubjectIdPropertyNotRoundTripped },
        { typeof(SubjectIsCryptoOwner), "Email", CryptoShreddedPropertyProblems.SubjectIdPropertyIsCryptoShredded },
        { typeof(GenericOwner<object>), "Note", CryptoShreddedPropertyProblems.SubjectIdTypeUnsupported },
    };

    [Theory]
    [MemberData(nameof(Rejected))]
    public void RejectedShape_IsReportedWithItsReason(Type type, string property, CryptoShreddedPropertyProblems expected)
    {
        var issues = type.IsInterface || type.IsValueType
            ? CryptoShreddedPropertyClassifier.GetShape(type).Issues
            : Classify(type);

        issues.ShouldContain(i => i.PropertyName == property && i.Problems.HasFlag(expected));
        issues.ShouldAllBe(i => !string.IsNullOrWhiteSpace(i.Describe()));
    }

    public static TheoryData<Type> Accepted => new()
    {
        typeof(TopLevelOwner), typeof(ContactInfo), typeof(NestedEvent), typeof(CollectionsEvent), typeof(DictionaryEvent),
        typeof(PolymorphicEvent), typeof(ContactNote), typeof(PositionalRecordOwner), typeof(WidePositionalRecordOwner),
        typeof(InitOwner), typeof(PrivateSetterOwner), typeof(NonPublicOwner), typeof(NonPublicDerived), typeof(GenericOwner<Guid>),
        typeof(GenericHolder), typeof(TwoSubjectEvent), typeof(VirtualOverrideOwner), typeof(SubjectTypedOwner), typeof(NoPiiEvent),
        typeof(PersonalDataSubjectOwner),
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public void AcceptedShape_HasNoIssue(Type type) => Classify(type).ShouldBeEmpty();

    [Fact]
    public void SeveralProblemsOnOneProperty_AreCombined()
    {
        var issue = CryptoShreddedPropertyClassifier.GetShape(typeof(StructOwner)).Issues.Single();

        issue.Problems.HasFlag(CryptoShreddedPropertyProblems.DeclaredOnValueType).ShouldBeTrue();
        CryptoShreddedPropertyClassifier.GetShape(typeof(NotStringOwner)).Issues.Single().Problems
            .ShouldBe(CryptoShreddedPropertyProblems.NotString);
    }

    [Fact]
    public void OpenGenericOwner_DefersTheSubjectIdTypeCheck()
    {
        var shape = CryptoShreddedPropertyClassifier.GetShape(typeof(GenericOwner<>));

        shape.Issues.ShouldBeEmpty();
        shape.Fields.ShouldBeEmpty();
        shape.DeferredChecks.ShouldBe(["Note"]);
    }

    [Fact]
    public void HidingWithADifferentType_ResolvesWithoutAmbiguity()
    {
        Should.NotThrow(() => CryptoShreddedPropertyClassifier.GetShape(typeof(HidingDerivedOwner)));
        CryptoShreddedPropertyClassifier.FindSubjectIdProperty(typeof(HidingDerivedOwner), "PatientId").ShouldNotBeNull();
    }

    [Fact]
    public void PrivateBaseProperty_IsAFieldWithACompiledSetter()
    {
        var field = CryptoShreddedPropertyClassifier.GetShape(typeof(NonPublicDerived)).Fields.Single();
        var owner = new NonPublicDerived { PatientId = "p" };

        field.Setter(owner, "v");

        owner.ReadHidden().ShouldBe("v");
        field.Getter(owner).ShouldBe("v");
        field.ResolveSubjectId(owner).ShouldBe("p");
    }

    [Fact]
    public void ReachesCryptoOwner_FollowsMembersCollectionsAndDerivedTypes()
    {
        CryptoShreddedPropertyClassifier.ReachesCryptoOwner(typeof(NestedEvent)).ShouldBeTrue();
        CryptoShreddedPropertyClassifier.ReachesCryptoOwner(typeof(List<ContactInfo>)).ShouldBeTrue();
        CryptoShreddedPropertyClassifier.ReachesCryptoOwner(typeof(NoteBase)).ShouldBeTrue();
        CryptoShreddedPropertyClassifier.ReachesCryptoOwner(typeof(NoPiiEvent)).ShouldBeFalse();
        CryptoShreddedPropertyClassifier.ReachesCryptoOwner(typeof(string)).ShouldBeFalse();
    }

    [Fact]
    public void ValueEquality_IsDetectedOnlyForOwners()
    {
        CryptoShreddedContractRules.HasValueEquality(typeof(RecordContact)).ShouldBeTrue();
        CryptoShreddedContractRules.HasValueEquality(typeof(ComparableContact)).ShouldBeTrue();
        CryptoShreddedContractRules.HasValueEquality(typeof(ContactInfo)).ShouldBeFalse();
        CryptoShreddedContractRules.HasValueEquality(typeof(NoPiiEvent)).ShouldBeFalse();
    }

    [Fact]
    public void SourceGeneratedContract_IsRejected()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = SerializationModeContext.Default };

        var issues = CryptoShreddedContractRules.Classify(
            options.GetTypeInfo(typeof(TopLevelOwner)), CryptoShreddedPropertyClassifier.GetShape(typeof(TopLevelOwner)));

        issues.ShouldContain(i => i.Problems.HasFlag(CryptoShreddedPropertyProblems.SourceGeneratedContract));
    }

    [Fact]
    public void SourceGeneratedContainer_IsRejected()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = SerializationModeContext.Default };

        var issues = CryptoShreddedContractRules.Classify(
            options.GetTypeInfo(typeof(NestedEvent)), CryptoShreddedPropertyClassifier.GetShape(typeof(NestedEvent)));

        issues.ShouldContain(i => i.Problems.HasFlag(CryptoShreddedPropertyProblems.SourceGeneratedContract));
    }

    [Fact]
    public void Describe_CoversEveryFlag()
    {
        foreach (var flag in Enum.GetValues<CryptoShreddedPropertyProblems>().Where(f => f != CryptoShreddedPropertyProblems.None))
        {
            CryptoShreddedPropertyIssue.DescribeFlag(flag).ShouldNotBe(flag.ToString());
        }

        var issue = new CryptoShreddedPropertyIssue("T", "P", CryptoShreddedPropertyProblems.NotString | CryptoShreddedPropertyProblems.NoSetter);
        issue.Describe().ShouldStartWith("T.P: ");
        issue.ToString().ShouldContain("NotString");
        CryptoShreddedPropertyIssue.DescribeFlag((CryptoShreddedPropertyProblems)(1 << 30)).ShouldBe("1073741824");
    }

    [Fact]
    public void RecordConstructorInspector_RejectsNonRecordsAndForeignProperties()
    {
        var constructor = typeof(JsonConstructorOwner).GetConstructors()[0];
        RecordConstructorInspector.StoresUnchanged(constructor, 1, typeof(JsonConstructorOwner).GetProperty("Email")!).ShouldBeFalse();

        var recordConstructor = typeof(PositionalRecordOwner).GetConstructor([typeof(string), typeof(string)])!;
        RecordConstructorInspector.StoresUnchanged(recordConstructor, 1, typeof(PositionalRecordOwner).GetProperty("Email")!).ShouldBeTrue();
        RecordConstructorInspector.StoresUnchanged(recordConstructor, 0, typeof(PositionalRecordOwner).GetProperty("Email")!).ShouldBeFalse();
        RecordConstructorInspector.StoresUnchanged(recordConstructor, 1, typeof(TopLevelOwner).GetProperty("Email")!).ShouldBeFalse();
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(TopLevelOwner))]
[JsonSerializable(typeof(NestedEvent))]
internal sealed partial class SerializationModeContext : JsonSerializerContext;
