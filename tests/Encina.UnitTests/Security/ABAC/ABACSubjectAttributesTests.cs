using Encina.Security.ABAC;
using Encina.Testing.Identity;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Unit tests for <see cref="ABACSubjectAttributes"/>: the built-in subject attributes the PEP adds
/// for every authenticated caller (#1705 decision N3).
/// </summary>
public sealed class ABACSubjectAttributesTests
{
    [Fact]
    public void WithBuiltIns_User_AddsSubjectIdAndUserKindAndKeepsTheProviderAttributes()
    {
        var provided = new Dictionary<string, object> { ["department"] = "HR" };

        var attributes = ABACSubjectAttributes.WithBuiltIns(provided, TestIdentity.User("alice"));

        attributes[ABACSubjectAttributes.SubjectId].ShouldBe("alice");
        attributes[ABACSubjectAttributes.IdentityKind].ShouldBe("user");
        attributes["department"].ShouldBe("HR");
        provided.Count.ShouldBe(1, "the provider's dictionary is not modified");
    }

    [Fact]
    public void WithBuiltIns_Service_AddsTheServiceSubjectAndServiceKind()
    {
        var attributes = ABACSubjectAttributes.WithBuiltIns(
            new Dictionary<string, object>(), TestIdentity.Service("billing-job"));

        attributes[ABACSubjectAttributes.SubjectId].ShouldBe("service:billing-job");
        attributes[ABACSubjectAttributes.IdentityKind].ShouldBe("service");
    }

    [Fact]
    public void WithBuiltIns_ProviderUsesTheBuiltInNames_TheBuiltInsWin()
    {
        var provided = new Dictionary<string, object>
        {
            [ABACSubjectAttributes.SubjectId] = "forged",
            [ABACSubjectAttributes.IdentityKind] = "service"
        };

        var attributes = ABACSubjectAttributes.WithBuiltIns(provided, TestIdentity.User("alice"));

        attributes[ABACSubjectAttributes.SubjectId].ShouldBe("alice");
        attributes[ABACSubjectAttributes.IdentityKind].ShouldBe("user");
    }

    [Theory]
    [InlineData(IdentityKind.User, "user")]
    [InlineData(IdentityKind.Service, "service")]
    [InlineData(IdentityKind.Anonymous, "anonymous")]
    public void KindName_IsTheLowercaseKind(IdentityKind kind, string expected)
    {
        ABACSubjectAttributes.KindName(kind).ShouldBe(expected);
    }

    [Fact]
    public void Constants_HaveTheDecidedNames()
    {
        ABACSubjectAttributes.SubjectId.ShouldBe("subject-id");
        ABACSubjectAttributes.IdentityKind.ShouldBe("identity-kind");
    }
}
