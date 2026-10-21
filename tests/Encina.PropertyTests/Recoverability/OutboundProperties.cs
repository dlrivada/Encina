using System.Globalization;
using Encina.Recoverability;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.Extensions.Time.Testing;

namespace Encina.PropertyTests.Recoverability;

/// <summary>
/// Invariants of the Retry-After parser and of the classification of outbound errors (#1223).
/// </summary>
public class OutboundProperties
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));
    private static readonly DefaultErrorClassifier Classifier = new();

    [FsCheck.Xunit.Property]
    public FsCheck.Property RetryAfter_DelaySeconds_RoundTrips() =>
        Prop.ForAll<NonNegativeInt>(seconds =>
            (RetryAfterParser.Parse(seconds.Get.ToString(CultureInfo.InvariantCulture), Time) == TimeSpan.FromSeconds(seconds.Get)).ToProperty());

    [FsCheck.Xunit.Property]
    public FsCheck.Property RetryAfter_AnyText_IsNeverNegative() =>
        Prop.ForAll<string>(text =>
            (RetryAfterParser.Parse(text, Time) is not { } delay || delay >= TimeSpan.Zero).ToProperty());

    [FsCheck.Xunit.Property]
    public FsCheck.Property RetryAfter_AnyHttpDate_IsNeverNegative() =>
        Prop.ForAll<NonNegativeInt>(offsetMinutes =>
        {
            var future = Time.GetUtcNow().AddMinutes(offsetMinutes.Get % 100000).ToString("R", CultureInfo.InvariantCulture);
            var past = Time.GetUtcNow().AddMinutes(-(offsetMinutes.Get % 100000)).ToString("R", CultureInfo.InvariantCulture);
            return (RetryAfterParser.Parse(future, Time) >= TimeSpan.Zero && RetryAfterParser.Parse(past, Time) >= TimeSpan.Zero).ToProperty();
        });

    [FsCheck.Xunit.Property]
    public FsCheck.Property Classification_DeclaredTransientMetadata_AlwaysWins() =>
        Prop.ForAll<bool, NonNull<string>>((transient, code) =>
        {
            var error = EncinaErrors.Create(
                code.Get, "m", new ArgumentException("permanent by type"),
                new Dictionary<string, object?> { [OutboundErrorMetadataKeys.Transient] = transient });

            var expected = transient ? ErrorClassification.Transient : ErrorClassification.Permanent;
            return (Classifier.Classify(error, new ArgumentException("x")) == expected).ToProperty();
        });

    [FsCheck.Xunit.Property]
    public FsCheck.Property Rules_AnOptInAlwaysAllowsTheRetry_AndNonAmbiguousErrorsAreAlwaysAllowed() =>
        Prop.ForAll<bool, bool, string>((ambiguous, optIn, key) =>
        {
            var details = new Dictionary<string, object?> { [OutboundErrorMetadataKeys.Transient] = true };
            if (ambiguous)
            {
                details[OutboundErrorMetadataKeys.Ambiguous] = true;
            }

            var error = EncinaErrors.Create(OutboundErrorCodes.ServerError, "m", details: details);
            var allowed = OutboundRetryRules.AllowsAutomaticRetry(error, key, optIn);
            var expected = !ambiguous || optIn || !string.IsNullOrEmpty(key);
            return (allowed == expected).ToProperty();
        });
}
