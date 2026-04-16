using Encina.Security.Sanitization.Attributes;
using Shouldly;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationAttributeTests
{
    #region SanitizationType Enum

    [Fact]
    public void SanitizationType_Html_HasValue0()
    {
        ((int)SanitizationType.Html).ShouldBe(0);
    }

    [Fact]
    public void SanitizationType_Sql_HasValue1()
    {
        ((int)SanitizationType.Sql).ShouldBe(1);
    }

    [Fact]
    public void SanitizationType_Shell_HasValue2()
    {
        ((int)SanitizationType.Shell).ShouldBe(2);
    }

    [Fact]
    public void SanitizationType_Custom_HasValue3()
    {
        ((int)SanitizationType.Custom).ShouldBe(3);
    }

    [Fact]
    public void SanitizationType_StripHtml_HasValue4()
    {
        ((int)SanitizationType.StripHtml).ShouldBe(4);
    }

    #endregion

    #region SanitizeHtmlAttribute

    [Fact]
    public void SanitizeHtmlAttribute_SanitizationType_IsHtml()
    {
        var attr = new SanitizeHtmlAttribute();

        attr.SanitizationType.ShouldBe(SanitizationType.Html);
    }

    [Fact]
    public void SanitizeHtmlAttribute_IsAttributeUsage_Property()
    {
        typeof(SanitizeHtmlAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false).ShouldNotBeEmpty();
    }

    #endregion

    #region SanitizeSqlAttribute

    [Fact]
    public void SanitizeSqlAttribute_SanitizationType_IsSql()
    {
        var attr = new SanitizeSqlAttribute();

        attr.SanitizationType.ShouldBe(SanitizationType.Sql);
    }

    #endregion

    #region SanitizeAttribute (Custom)

    [Fact]
    public void SanitizeAttribute_SanitizationType_IsCustom()
    {
        var attr = new SanitizeAttribute();

        attr.SanitizationType.ShouldBe(SanitizationType.Custom);
    }

    [Fact]
    public void SanitizeAttribute_Profile_DefaultIsNull()
    {
        var attr = new SanitizeAttribute();

        attr.Profile.ShouldBeNull();
    }

    [Fact]
    public void SanitizeAttribute_Profile_CanBeSet()
    {
        var attr = new SanitizeAttribute { Profile = "BlogPost" };

        attr.Profile.ShouldBe("BlogPost");
    }

    #endregion

    #region StripHtmlAttribute

    [Fact]
    public void StripHtmlAttribute_SanitizationType_IsStripHtml()
    {
        var attr = new StripHtmlAttribute();

        attr.SanitizationType.ShouldBe(SanitizationType.StripHtml);
    }

    #endregion

    #region EncodingContext Enum

    [Fact]
    public void EncodingContext_Html_HasValue0()
    {
        ((int)EncodingContext.Html).ShouldBe(0);
    }

    [Fact]
    public void EncodingContext_JavaScript_HasValue1()
    {
        ((int)EncodingContext.JavaScript).ShouldBe(1);
    }

    [Fact]
    public void EncodingContext_Url_HasValue2()
    {
        ((int)EncodingContext.Url).ShouldBe(2);
    }

    #endregion

    #region EncodeForHtmlAttribute

    [Fact]
    public void EncodeForHtmlAttribute_EncodingContext_IsHtml()
    {
        var attr = new EncodeForHtmlAttribute();

        attr.EncodingContext.ShouldBe(EncodingContext.Html);
    }

    #endregion

    #region EncodeForJavaScriptAttribute

    [Fact]
    public void EncodeForJavaScriptAttribute_EncodingContext_IsJavaScript()
    {
        var attr = new EncodeForJavaScriptAttribute();

        attr.EncodingContext.ShouldBe(EncodingContext.JavaScript);
    }

    #endregion

    #region EncodeForUrlAttribute

    [Fact]
    public void EncodeForUrlAttribute_EncodingContext_IsUrl()
    {
        var attr = new EncodeForUrlAttribute();

        attr.EncodingContext.ShouldBe(EncodingContext.Url);
    }

    #endregion
}
