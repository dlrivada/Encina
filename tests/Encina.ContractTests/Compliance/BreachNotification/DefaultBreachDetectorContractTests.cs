#pragma warning disable CA2012

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection;
using Encina.Compliance.BreachNotification.Model;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying <see cref="DefaultBreachDetector"/> behavioral contract:
/// detection with no rules returns empty, registered rules are queryable.
/// </summary>
[Trait("Category", "Contract")]
public class DefaultBreachDetectorContractTests
{
    [Fact]
    public async Task Contract_DetectAsync_NoRules_ReturnsEmptyList()
    {
        var detector = new DefaultBreachDetector(
            [],
            NullLogger<DefaultBreachDetector>.Instance);

        var securityEvent = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess, "TestSource", "Test event", DateTimeOffset.UtcNow);

        var result = await detector.DetectAsync(securityEvent);

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: breaches => breaches.Count.ShouldBe(0),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Contract_GetRegisteredRulesAsync_ReturnsRuleNames()
    {
        var detector = new DefaultBreachDetector(
            [],
            NullLogger<DefaultBreachDetector>.Instance);

        var result = await detector.GetRegisteredRulesAsync();

        result.IsRight.ShouldBeTrue();
    }
}
