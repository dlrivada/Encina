using Encina.Security.Sanitization.Attributes;
using FluentAssertions;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationAttributeTests
{
    #region SanitizationType Enum

    [Fact]
    public void SanitizationType_Html_HasValue0()
    {
        ((int)SanitizationType.Html).Should().Be(0);
    }

    [Fact]
    public void SanitizationType_Sql_HasValue1()
    {
        ((int)SanitizationType.Sql).Should().Be(1);
    }

    [Fact]
    public void SanitizationType_Shell_HasValue2()
    {
        ((int)SanitizationType.Shell).Should().Be(2);
    }

    [Fact]
    public void SanitizationType_Custom_HasValue3()
    {
        ((int)SanitizationType.Custom).Should().Be(3);
    }

    [Fact]
    public void SanitizationType_StripHtml_HasValue4()
    {
        ((int)SanitizationType.StripHtml).Should().Be(4);
    }

    #endregion

    #region SanitizeHtmlAttribute

    [Fact]
    public void SanitizeHtmlAttribute_SanitizationType_IsHtml()
    {
        var attr = new SanitizeHtmlAttribute();

        attr.SanitizationType.Should().Be(SanitizationType.Html);
    }

    [Fact]
    public void SanitizeHtmlAttribute_IsAttributeUsage_Property()
    {
        typeof(SanitizeHtmlAttribute).Should().BeDecoratedWith<AttributeUsageAttribute>();
    }

    #endregion

    #region SanitizeSqlAttribute

    [Fact]
    public void SanitizeSqlAttribute_SanitizationType_IsSql()
    {
        var attr = new SanitizeSqlAttribute();

        attr.SanitizationType.Should().Be(SanitizationType.Sql);
    }

    #endregion

    #region SanitizeAttribute (Custom)

    [Fact]
    public void SanitizeAttribute_SanitizationType_IsCustom()
    {
        var attr = new SanitizeAttribute();

        attr.SanitizationType.Should().Be(SanitizationType.Custom);
    }

    [Fact]
    public void SanitizeAttribute_Profile_DefaultIsNull()
    {
        var attr = new SanitizeAttribute();

        attr.Profile.Should().BeNull();
    }

    [Fact]
    public void SanitizeAttribute_Profile_CanBeSet()
    {
        var attr = new SanitizeAttribute { Profile = "BlogPost" };

        attr.Profile.Should().Be("BlogPost");
    }

    #endregion

    #region StripHtmlAttribute

    [Fact]
    public void StripHtmlAttribute_SanitizationType_IsStripHtml()
    {
        var attr = new StripHtmlAttribute();

        attr.SanitizationType.Should().Be(SanitizationType.StripHtml);
    }

    #endregion

    #region EncodingContext Enum

    [Fact]
    public void EncodingContext_Html_HasValue0()
    {
        ((int)EncodingContext.Html).Should().Be(0);
    }

    [Fact]
    public void EncodingContext_JavaScript_HasValue1()
    {
        ((int)EncodingContext.JavaScript).Should().Be(1);
    }

    [Fact]
    public void EncodingContext_Url_HasValue2()
    {
        ((int)EncodingContext.Url).Should().Be(2);
    }

    #endregion

    #region EncodeForHtmlAttribute

    [Fact]
    public void EncodeForHtmlAttribute_EncodingContext_IsHtml()
    {
        var attr = new EncodeForHtmlAttribute();

        attr.EncodingContext.Should().Be(EncodingContext.Html);
    }

    #endregion

    #region EncodeForJavaScriptAttribute

    [Fact]
    public void EncodeForJavaScriptAttribute_EncodingContext_IsJavaScript()
    {
        var attr = new EncodeForJavaScriptAttribute();

        attr.EncodingContext.Should().Be(EncodingContext.JavaScript);
    }

    #endregion

    #region EncodeForUrlAttribute

    [Fact]
    public void EncodeForUrlAttribute_EncodingContext_IsUrl()
    {
        var attr = new EncodeForUrlAttribute();

        attr.EncodingContext.Should().Be(EncodingContext.Url);
    }

    #endregion
}
