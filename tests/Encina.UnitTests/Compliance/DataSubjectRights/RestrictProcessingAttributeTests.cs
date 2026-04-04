using Encina.Compliance.DataSubjectRights;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="RestrictProcessingAttribute"/>.
/// </summary>
public class RestrictProcessingAttributeTests
{
    [Fact]
    public void SubjectIdProperty_Default_IsNull()
    {
        var attr = new RestrictProcessingAttribute();
        attr.SubjectIdProperty.ShouldBeNull();
    }

    [Fact]
    public void SubjectIdProperty_WhenSet_ReturnsValue()
    {
        var attr = new RestrictProcessingAttribute { SubjectIdProperty = "CustomerId" };
        attr.SubjectIdProperty.ShouldBe("CustomerId");
    }
}
