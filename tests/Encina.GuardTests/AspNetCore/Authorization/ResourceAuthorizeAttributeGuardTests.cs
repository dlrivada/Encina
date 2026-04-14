using Encina.AspNetCore.Authorization;

namespace Encina.GuardTests.AspNetCore.Authorization;

public class ResourceAuthorizeAttributeGuardTests
{
    [Fact]
    public void Constructor_NullPolicy_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute(null!));
    }

    [Fact]
    public void Constructor_EmptyPolicy_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute(""));
    }

    [Fact]
    public void Constructor_WhitespacePolicy_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new ResourceAuthorizeAttribute("  "));
    }

    [Fact]
    public void Constructor_ValidPolicy_SetsProperty()
    {
        var attr = new ResourceAuthorizeAttribute("P");
        attr.Policy.ShouldBe("P");
    }
}