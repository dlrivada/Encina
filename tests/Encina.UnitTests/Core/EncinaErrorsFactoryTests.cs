using LanguageExt;
using static LanguageExt.Prelude;
using Shouldly;

namespace Encina.UnitTests.Core;

public sealed class EncinaErrorsFactoryTests
{
    [Fact]
    public void Validation_CreatesErrorWithValidationCategoryAndCode()
    {
        var details = new Dictionary<string, object?> { ["field"] = "Name" };
        var error = EncinaErrors.Validation("Validation failed", details);

        error.Message.ShouldBe("Validation failed");
        error.GetCategory().ShouldBe(ErrorCategory.Validation);
        error.GetCode().ShouldBe(Some(EncinaErrorCodes.ValidationFailed));
        error.GetDetails().ShouldBe(details);
    }

    [Fact]
    public void NotFound_CreatesErrorWithNotFoundCategoryAndCode()
    {
        var details = new Dictionary<string, object?> { ["id"] = 123 };
        var error = EncinaErrors.NotFound("Order not found", details);

        error.Message.ShouldBe("Order not found");
        error.GetCategory().ShouldBe(ErrorCategory.NotFound);
        error.GetCode().ShouldBe(Some(EncinaErrorCodes.NotFound));
        error.GetDetails().ShouldBe(details);
    }
}
