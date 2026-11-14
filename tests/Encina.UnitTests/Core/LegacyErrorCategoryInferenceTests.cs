using Shouldly;

namespace Encina.UnitTests.Core;

/// <summary>
/// Unit tests of <see cref="LegacyErrorCategoryInference"/>: the hosts' former suffix and prefix rules, which keep the
/// HTTP status of every producer that has not declared a category yet (ADR-062, #3545). One row per branch.
/// </summary>
public sealed class LegacyErrorCategoryInferenceTests
{
    [Theory]
    // Validation (400): the "validation." prefix and the guard code.
    [InlineData("validation.required", ErrorCategory.Validation)]
    [InlineData("Validation.Required", ErrorCategory.Validation)]
    [InlineData("encina.guard.validation_failed", ErrorCategory.Validation)]
    // Unauthenticated (401): exactly one code.
    [InlineData(EncinaErrorCodes.AuthorizationUnauthenticated, ErrorCategory.Unauthenticated)]
    // Forbidden (403): every other encina.authorization.* code, even one that ends in .not_found.
    [InlineData(EncinaErrorCodes.AuthorizationForbidden, ErrorCategory.Forbidden)]
    [InlineData(EncinaErrorCodes.AuthorizationUnauthorized, ErrorCategory.Forbidden)]
    [InlineData("encina.authorization.policy.not_found", ErrorCategory.Forbidden)]
    // NotFound (404): ".not_found", the handler-missing code and ".missing".
    [InlineData("orders.not_found", ErrorCategory.NotFound)]
    [InlineData("encina.request.handler_missing", ErrorCategory.NotFound)]
    [InlineData("consent.missing", ErrorCategory.NotFound)]
    // Conflict (409).
    [InlineData("orders.conflict", ErrorCategory.Conflict)]
    // AlreadyExists (409): ".already_exists" and ".duplicate".
    [InlineData("processor.already_exists", ErrorCategory.AlreadyExists)]
    [InlineData("gdpr.processing_activity.duplicate", ErrorCategory.AlreadyExists)]
    // Internal (500): everything else, including the near misses the suffix rules never matched.
    [InlineData("repository.notfound", ErrorCategory.Internal)]
    [InlineData("gdpr.processing_activity_duplicate", ErrorCategory.Internal)]
    [InlineData("validation", ErrorCategory.Internal)]
    [InlineData("Encina.unknown", ErrorCategory.Internal)]
    [InlineData("", ErrorCategory.Internal)]
    [InlineData(null, ErrorCategory.Internal)]
    public void Infer_MapsTheCodeTextToTheCategoryOfItsFormerStatus(string? code, ErrorCategory expected)
    {
        LegacyErrorCategoryInference.Infer(code).ShouldBe(expected);
    }

    [Fact]
    public void Infer_ValidationWinsOverASuffixRule()
    {
        // Rule order is the former mapper's: validation is checked before not-found.
        LegacyErrorCategoryInference.Infer("validation.field.not_found").ShouldBe(ErrorCategory.Validation);
    }
}
