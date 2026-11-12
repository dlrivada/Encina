using FsCheck;
using FsCheck.Fluent;

namespace Encina.PropertyTests.Core;

/// <summary>
/// Property tests of the error category contract (ADR-062, #3545): any code and any category round-trip through
/// <see cref="EncinaErrorExtensions.GetCode(EncinaError)"/> and <see cref="EncinaErrorExtensions.GetCategory(EncinaError)"/>;
/// a code-less error is always <see cref="ErrorCategory.Internal"/>; and a category-less error answers what the
/// transitional inference says, whatever the letter case of its code.
/// </summary>
public sealed class ErrorCategoryProperties
{
    private static readonly string[] Prefixes = ["", "orders", "validation", "encina.authorization", "encina.request", "Encina", "VALIDATION"];

    private static readonly string[] Suffixes =
        ["", ".not_found", ".missing", ".conflict", ".already_exists", ".duplicate", ".handler_missing", ".unauthenticated", ".failed", ".NOT_FOUND"];

    private static Arbitrary<string> CodeArb() => Arb.From(
        from prefix in Gen.Elements(Prefixes)
        from separator in Gen.Elements("", ".")
        from middle in Gen.Elements("", "x", "required", "policy")
        from suffix in Gen.Elements(Suffixes)
        select prefix + separator + middle + suffix);

    private static Arbitrary<ErrorCategory> CategoryArb() => Arb.From(Gen.Elements(Enum.GetValues<ErrorCategory>()));

    [Fact]
    public void Create_AnyCodeAndCategory_RoundTrips()
    {
        Prop.ForAll(CodeArb(), CategoryArb(), (code, category) =>
        {
            var error = EncinaErrors.Create(code, category, "message");

            return error.GetCategory() == category && error.GetCode().IfNone("<none>") == code;
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void FromException_AnyCodeAndCategory_RoundTrips()
    {
        Prop.ForAll(CodeArb(), CategoryArb(), (code, category) =>
        {
            var error = EncinaErrors.FromException(code, category, new InvalidOperationException("boom"));

            return error.GetCategory() == category && error.GetCode().IfNone("<none>") == code;
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void ACodelessError_IsAlwaysInternal()
    {
        Prop.ForAll(CodeArb(), message => EncinaError.New(message).GetCategory() == ErrorCategory.Internal)
            .QuickCheckThrowOnFailure();
    }

    [Fact]
    public void ACategorylessError_AnswersTheInference_IgnoringCase()
    {
        Prop.ForAll(CodeArb(), code =>
        {
            var inferred = EncinaErrors.Create(code, "message").GetCategory();

            return inferred == LegacyErrorCategoryInference.Infer(code)
                && inferred == LegacyErrorCategoryInference.Infer(code.ToUpperInvariant())
                && inferred == LegacyErrorCategoryInference.Infer(code.ToLowerInvariant());
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void TheInference_NeverAnswersACategoryItDoesNotOwn()
    {
        // The inference only reproduces the former 400/401/403/404/409/500 answers; every other category is declared.
        ErrorCategory[] inferable =
        [
            ErrorCategory.Validation, ErrorCategory.Unauthenticated, ErrorCategory.Forbidden, ErrorCategory.NotFound,
            ErrorCategory.Conflict, ErrorCategory.AlreadyExists, ErrorCategory.Internal,
        ];

        Prop.ForAll(CodeArb(), code => inferable.Contains(LegacyErrorCategoryInference.Infer(code)))
            .QuickCheckThrowOnFailure();
    }
}
