using System.Reflection;

using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Abstractions;
using Encina.Compliance.Consent.ReadModels;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Time.Testing;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method

namespace Encina.PropertyTests.Compliance.Consent;

/// <summary>
/// Property-based tests for the consent validation logic: <see cref="ConsentOptionsValidator"/>,
/// <see cref="DefaultConsentValidator"/> and <see cref="SubjectIdConversion"/>. Each property runs the real
/// implementation; only the consent store behind <see cref="IConsentService"/> is substituted.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "Consent")]
public class ConsentValidationPropertyTests
{
    private sealed class SubjectHolder
    {
        public string? Text { get; set; }

        public long Number { get; set; }

        public Guid Identifier { get; set; }
    }

    private static readonly PropertyInfo TextProperty = typeof(SubjectHolder).GetProperty(nameof(SubjectHolder.Text))!;
    private static readonly PropertyInfo NumberProperty = typeof(SubjectHolder).GetProperty(nameof(SubjectHolder.Number))!;
    private static readonly PropertyInfo IdentifierProperty = typeof(SubjectHolder).GetProperty(nameof(SubjectHolder.Identifier))!;

    // ---- ConsentOptionsValidator ----

    /// <summary>Invariant: a non-positive default expiration is always rejected.</summary>
    [Property(MaxTest = 100)]
    public bool OptionsValidator_NonPositiveExpiration_AlwaysFails(NonNegativeInt magnitude)
    {
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Disabled,
            DefaultExpirationDays = -magnitude.Get
        };

        return new ConsentOptionsValidator().Validate(null, options).Failed;
    }

    /// <summary>Invariant: a positive default expiration is always accepted when nothing else is wrong.</summary>
    [Property(MaxTest = 100)]
    public bool OptionsValidator_PositiveExpiration_Succeeds(PositiveInt days)
    {
        var options = new ConsentOptions
        {
            EnforcementMode = ConsentEnforcementMode.Disabled,
            DefaultExpirationDays = days.Get
        };

        return new ConsentOptionsValidator().Validate(null, options).Succeeded;
    }

    /// <summary>Invariant: Block mode needs at least one declared purpose; with one it is accepted.</summary>
    [Property(MaxTest = 100)]
    public bool OptionsValidator_BlockMode_SucceedsExactlyWhenAPurposeIsDefined(NonEmptyString purpose, bool defined)
    {
        var options = new ConsentOptions { EnforcementMode = ConsentEnforcementMode.Block };
        if (defined)
        {
            options.PurposeDefinitions.Add(purpose.Get);
        }

        return new ConsentOptionsValidator().Validate(null, options).Succeeded == defined;
    }

    /// <summary>Invariant: a detailed purpose with a non-positive expiration is always rejected.</summary>
    [Property(MaxTest = 100)]
    public bool OptionsValidator_DetailedPurposeWithNonPositiveExpiration_Fails(NonEmptyString purpose, NonNegativeInt magnitude)
    {
        if (string.IsNullOrWhiteSpace(purpose.Get))
        {
            return true; // DefinePurpose rejects blank identifiers by contract (covered by a guard test)
        }

        var options = new ConsentOptions { EnforcementMode = ConsentEnforcementMode.Disabled };
        options.DefinePurpose(purpose.Get, entry => entry.DefaultExpirationDays = -magnitude.Get);

        return new ConsentOptionsValidator().Validate(null, options).Failed;
    }

    // ---- SubjectIdConversion ----

    /// <summary>Invariant: a non-blank string subject is returned unchanged.</summary>
    [Property(MaxTest = 100)]
    public bool SubjectIdConversion_NonBlankString_IsReturnedUnchanged(NonEmptyString text)
    {
        if (string.IsNullOrWhiteSpace(text.Get))
        {
            return true;
        }

        return SubjectIdConversion.ToInvariantString(text.Get, TextProperty) == text.Get;
    }

    /// <summary>Invariant: an integer subject converts to its invariant decimal form, so 0 is a valid subject.</summary>
    [Property(MaxTest = 100)]
    public bool SubjectIdConversion_Integer_UsesInvariantDecimalForm(long number) =>
        SubjectIdConversion.ToInvariantString(number, NumberProperty)
            == number.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Invariant: a non-empty Guid converts to the "D" format; the empty Guid means a missing subject.</summary>
    [Property(MaxTest = 100)]
    public bool SubjectIdConversion_Guid_UsesDFormatAndEmptyIsMissing(Guid identifier)
    {
        var converted = SubjectIdConversion.ToInvariantString(identifier, IdentifierProperty);

        return identifier == Guid.Empty
            ? converted is null
            : converted == identifier.ToString("D");
    }

    /// <summary>Invariant: a null subject is a missing subject.</summary>
    [Fact]
    public void SubjectIdConversion_NullValue_IsMissing() =>
        SubjectIdConversion.ToInvariantString(null, TextProperty).ShouldBeNull();

    // ---- DefaultConsentValidator ----

    /// <summary>
    /// Invariant: a purpose is accepted exactly when its consent is Active (or otherwise not Withdrawn, Expired or
    /// RequiresReconsent) and has not passed its expiry; every rejected purpose is reported as missing.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task<bool> DefaultValidator_PurposeIsValidExactlyWhenConsentIsUsable(
        PositiveInt expiryOffsetHours, bool expiresInTheFuture, byte statusSeed)
    {
        var statuses = Enum.GetValues<ConsentStatus>();
        var status = statuses[statusSeed % statuses.Length];
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var expiry = time.GetUtcNow().AddHours(expiresInTheFuture ? expiryOffsetHours.Get : -expiryOffsetHours.Get);
        var model = new ConsentReadModel { Id = Guid.NewGuid(), DataSubjectId = "s", Purpose = "p", Status = status, ExpiresAtUtc = expiry };
        var service = Substitute.For<IConsentService>();
        service.GetConsentBySubjectAndPurposeAsync("s", "p", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ConsentReadModel>>>(
                Right<EncinaError, Option<ConsentReadModel>>(Some(model))));

        var outcome = await new DefaultConsentValidator(service, time).ValidateAsync("s", ["p"]);

        var result = outcome.Match(r => r, _ => null!);
        var usable = status is not (ConsentStatus.Withdrawn or ConsentStatus.Expired or ConsentStatus.RequiresReconsent)
            && expiresInTheFuture;

        return result.IsValid == usable && result.MissingPurposes.Count == (usable ? 0 : 1);
    }

    /// <summary>Invariant: a purpose with no consent record is always reported missing.</summary>
    [Property(MaxTest = 100)]
    public async Task<bool> DefaultValidator_PurposeWithoutRecord_IsAlwaysMissing(NonEmptyString purpose)
    {
        var service = Substitute.For<IConsentService>();
        service.GetConsentBySubjectAndPurposeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ConsentReadModel>>>(
                Right<EncinaError, Option<ConsentReadModel>>(None)));

        var outcome = await new DefaultConsentValidator(service, TimeProvider.System).ValidateAsync("s", [purpose.Get]);

        var result = outcome.Match(r => r, _ => null!);
        return !result.IsValid && result.MissingPurposes.SequenceEqual([purpose.Get]);
    }

    /// <summary>Invariant: a store failure is propagated and never reported as a valid consent.</summary>
    [Property(MaxTest = 100)]
    public async Task<bool> DefaultValidator_StoreFailure_IsNeverReportedAsValid(NonEmptyString purpose)
    {
        var service = Substitute.For<IConsentService>();
        service.GetConsentBySubjectAndPurposeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<ConsentReadModel>>>(
                Left<EncinaError, Option<ConsentReadModel>>(EncinaErrors.Create("store.down", "down"))));

        var outcome = await new DefaultConsentValidator(service, TimeProvider.System).ValidateAsync("s", [purpose.Get]);

        return outcome.IsLeft;
    }
}
