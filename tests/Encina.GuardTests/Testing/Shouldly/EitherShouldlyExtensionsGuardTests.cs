using LanguageExt;

namespace Encina.GuardTests.Testing.Shouldly;

/// <summary>
/// Guard tests for the category-based assertions of <see cref="EitherShouldlyExtensions"/>:
/// the nullable custom message accepts null and the assertion still returns the error.
/// </summary>
public sealed class EitherShouldlyExtensionsGuardTests
{
    [Fact]
    public void ShouldBeValidationError_NullCustomMessage_ReturnsError()
    {
        Either<EncinaError, int> result = EncinaErrors.Validation("invalid");

        var error = result.ShouldBeValidationError(customMessage: null);

        error.GetCategory().ShouldBe(ErrorCategory.Validation);
    }

    [Fact]
    public void ShouldBeNotFoundError_NullCustomMessage_ReturnsError()
    {
        Either<EncinaError, int> result = EncinaErrors.Create("orders.not_found", ErrorCategory.NotFound, "missing");

        var error = result.ShouldBeNotFoundError(customMessage: null);

        error.GetCategory().ShouldBe(ErrorCategory.NotFound);
    }

    [Fact]
    public async Task ShouldContainValidationErrorForAsync_NullCustomMessage_ReturnsError()
    {
        var validation = EncinaErrors.Validation("Email is invalid");

        var error = await Stream(validation).ShouldContainValidationErrorForAsync("Email", customMessage: null);

        error.GetCategory().ShouldBe(ErrorCategory.Validation);
    }

    private static async IAsyncEnumerable<Either<EncinaError, int>> Stream(EncinaError error)
    {
        yield return error;
        await Task.Yield();
    }
}
