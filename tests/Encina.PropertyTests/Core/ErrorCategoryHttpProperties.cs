using FsCheck;
using FsCheck.Fluent;

namespace Encina.PropertyTests.Core;

/// <summary>
/// Property tests of the HTTP column of the error category contract (ADR-062, #3683): every category maps to a client
/// or server error status, an undefined value never throws and <c>Retry-After</c> is written only with 429 or 503.
/// </summary>
public sealed class ErrorCategoryHttpProperties
{
    private static Arbitrary<ErrorCategory> CategoryArb() => Arb.From(Gen.Elements(Enum.GetValues<ErrorCategory>()));

    private static Arbitrary<int> StatusArb() => Arb.From(Gen.Choose(0, 700));

    [Fact]
    public void EveryCategory_MapsToAStatusBetween400And599()
    {
        Prop.ForAll(CategoryArb(), category =>
        {
            var status = ErrorCategoryHttp.GetStatusCode(category);

            return status is >= 400 and <= 599;
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void AnyIntegerValue_AsACategory_MapsToAStatusBetween400And599()
    {
        Prop.ForAll(Arb.From(Gen.Choose(-100, 100)), value =>
        {
            var status = ErrorCategoryHttp.GetStatusCode((ErrorCategory)value);

            return status is >= 400 and <= 599;
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void RetryAfter_IsWrittenOnlyWith429Or503()
    {
        Prop.ForAll(CategoryArb(), StatusArb(), Arb.From(Gen.Choose(0, 100_000)), (category, status, millis) =>
        {
            var details = new Dictionary<string, object?>
            {
                [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromMilliseconds(millis),
            };
            var error = EncinaErrors.Create("test.error", category, "message", details: details);

            var written = ErrorCategoryHttp.GetResponseHeaders(error, status).Any(h => h.Key == "Retry-After");

            return written == (status is 429 or 503);
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Every401_CarriesExactlyOneNonBlankChallenge()
    {
        Prop.ForAll(CategoryArb(), category =>
        {
            var headers = ErrorCategoryHttp.GetResponseHeaders(EncinaErrors.Create("test.error", category, "message"), 401);

            return headers.Count == 1 && headers[0].Key == "WWW-Authenticate" && !string.IsNullOrWhiteSpace(headers[0].Value);
        }).QuickCheckThrowOnFailure();
    }
}
