using Encina.Compliance.GDPR;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="DefaultGDPRComplianceValidator"/>.
/// </summary>
public class DefaultGDPRComplianceValidatorTests
{
    private readonly DefaultGDPRComplianceValidator _sut = new();

    [Fact]
    public async Task ValidateAsync_AnyRequest_ShouldReturnCompliant()
    {
        // Arrange
        var request = new SampleNoAttributeRequest();
        var context = RequestContext.CreateForTest();

        // Act
        var result = await _sut.ValidateAsync(request, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = (ComplianceResult)result;
        compliance.IsCompliant.ShouldBeTrue();
        compliance.Errors.ShouldBeEmpty();
        compliance.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_ShouldReturnCompliant()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var request = new SampleDecoratedRequest();
        var context = RequestContext.CreateForTest();

        // Act
        var result = await _sut.ValidateAsync(request, context, cts.Token);

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = (ComplianceResult)result;
        compliance.IsCompliant.ShouldBeTrue();
    }
}
