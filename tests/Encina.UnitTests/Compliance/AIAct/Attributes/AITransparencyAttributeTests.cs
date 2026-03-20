using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.AIAct.Attributes;

/// <summary>
/// Unit tests for <see cref="AITransparencyAttribute"/>.
/// </summary>
public class AITransparencyAttributeTests
{
    [Fact]
    public void DisclosureText_ShouldBeSetViaConstructor()
    {
        var attr = new AITransparencyAttribute("This is AI-generated content.");
        attr.DisclosureText.Should().Be("This is AI-generated content.");
    }

    [Fact]
    public void ObligationType_ShouldDefaultToAIGeneratedContent()
    {
        var attr = new AITransparencyAttribute("Test");
        attr.ObligationType.Should().Be(TransparencyObligationType.AIGeneratedContent);
    }

    [Fact]
    public void ObligationType_ShouldBeSettable()
    {
        var attr = new AITransparencyAttribute("You are talking to AI.")
        {
            ObligationType = TransparencyObligationType.ChatbotInteraction
        };
        attr.ObligationType.Should().Be(TransparencyObligationType.ChatbotInteraction);
    }
}
