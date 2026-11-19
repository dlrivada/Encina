using static LanguageExt.Prelude;

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
}
