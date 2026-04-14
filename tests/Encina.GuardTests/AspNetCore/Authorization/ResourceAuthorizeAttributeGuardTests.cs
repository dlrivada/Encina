using Encina.AspNetCore.Authorization;

namespace Encina.GuardTests.AspNetCore.Authorization;

public class ResourceAuthorizeAttributeGuardTests
{
    [Fact]
    public void Constructor_ValidPolicy_SetsPolicy()
    {
        var attr = new ResourceAuthorizeAttribute("TestPolicy");
        attr.Policy.ShouldBe("TestPolicy");
    }

    [Fact]
    public void Constructor_NullPolicy_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute(null!));
        ex.ParamName.ShouldBe("policy");
    }

    [Fact]
    public void Constructor_EmptyPolicy_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute(string.Empty));
        ex.ParamName.ShouldBe("policy");
    }

    [Fact]
    public void Constructor_WhitespacePolicy_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute("   "));
        ex.ParamName.ShouldBe("policy");
    }
}
