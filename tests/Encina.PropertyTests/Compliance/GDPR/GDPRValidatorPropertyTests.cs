using Encina.Compliance.GDPR;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Compliance.GDPR;

/// <summary>
/// Property-based tests for <c>GDPROptionsValidator</c> and <see cref="DefaultGDPRComplianceValidator"/>.
/// </summary>
public class GDPRValidatorPropertyTests
{
    private static readonly Gen<string> BlankText = Gen.Elements("", " ", "  ", "\t", " \t ");

    private static string Filled(NonEmptyString text) => "x" + text.Get;

    private static ValidateOptionsResult Validate(GDPROptions options) =>
        new GDPROptionsValidator().Validate(Options.DefaultName, options);

    // -- GDPROptionsValidator: valid configurations --

    [Property(MaxTest = 50)]
    public bool OptionsValidator_EnforceWithControllerAndDpo_AlwaysSucceeds(
        NonEmptyString name, NonEmptyString email, NonEmptyString dpoName, NonEmptyString dpoEmail)
    {
        var options = new GDPROptions
        {
            EnforcementMode = GDPREnforcementMode.Enforce,
            ControllerName = Filled(name),
            ControllerEmail = Filled(email),
            DataProtectionOfficer = new DataProtectionOfficer(Filled(dpoName), Filled(dpoEmail))
        };

        return Validate(options).Succeeded;
    }

    [Property(MaxTest = 50)]
    public Property OptionsValidator_WarnOnlyWithBlankController_AlwaysSucceeds() =>
        Prop.ForAll(
            BlankText.ToArbitrary(),
            BlankText.ToArbitrary(),
            (name, email) => Validate(new GDPROptions
            {
                EnforcementMode = GDPREnforcementMode.WarnOnly,
                ControllerName = name,
                ControllerEmail = email
            }).Succeeded);

    // -- GDPROptionsValidator: each invalid field fails --

    [Property(MaxTest = 50)]
    public Property OptionsValidator_EnforceWithBlankControllerName_AlwaysFailsOnThatField() =>
        Prop.ForAll(
            BlankText.ToArbitrary(),
            (name) =>
            {
                var result = Validate(new GDPROptions { ControllerName = name, ControllerEmail = "a@b.test" });
                return result.Failed && result.Failures!.Count() == 1 && result.FailureMessage.Contains("ControllerName");
            });

    [Property(MaxTest = 50)]
    public Property OptionsValidator_EnforceWithBlankControllerEmail_AlwaysFailsOnThatField() =>
        Prop.ForAll(
            BlankText.ToArbitrary(),
            (email) =>
            {
                var result = Validate(new GDPROptions { ControllerName = "Acme", ControllerEmail = email });
                return result.Failed && result.Failures!.Count() == 1 && result.FailureMessage.Contains("ControllerEmail");
            });

    [Property(MaxTest = 50)]
    public Property OptionsValidator_BlankDpoName_AlwaysFailsEvenInWarnOnly() =>
        Prop.ForAll(
            BlankText.ToArbitrary(),
            Gen.Elements(GDPREnforcementMode.Enforce, GDPREnforcementMode.WarnOnly).ToArbitrary(),
            (dpoName, mode) =>
            {
                var result = Validate(new GDPROptions
                {
                    EnforcementMode = mode,
                    ControllerName = "Acme",
                    ControllerEmail = "a@b.test",
                    DataProtectionOfficer = new DataProtectionOfficer(dpoName, "dpo@b.test")
                });
                return result.Failed && result.FailureMessage.Contains("DataProtectionOfficer.Name");
            });

    [Property(MaxTest = 50)]
    public Property OptionsValidator_BlankDpoEmail_AlwaysFailsEvenInWarnOnly() =>
        Prop.ForAll(
            BlankText.ToArbitrary(),
            Gen.Elements(GDPREnforcementMode.Enforce, GDPREnforcementMode.WarnOnly).ToArbitrary(),
            (dpoEmail, mode) =>
            {
                var result = Validate(new GDPROptions
                {
                    EnforcementMode = mode,
                    ControllerName = "Acme",
                    ControllerEmail = "a@b.test",
                    DataProtectionOfficer = new DataProtectionOfficer("Dana", dpoEmail)
                });
                return result.Failed && result.FailureMessage.Contains("DataProtectionOfficer.Email");
            });

    [Property(MaxTest = 50)]
    public Property OptionsValidator_FailureCount_EqualsNumberOfInvalidFields() =>
        Prop.ForAll(
            Gen.Choose(0, 15).ToArbitrary(),
            invalidMask =>
            {
                var badName = (invalidMask & 1) != 0;
                var badEmail = (invalidMask & 2) != 0;
                var badDpoName = (invalidMask & 4) != 0;
                var badDpoEmail = (invalidMask & 8) != 0;
                var result = Validate(new GDPROptions
                {
                    EnforcementMode = GDPREnforcementMode.Enforce,
                    ControllerName = badName ? " " : "Acme",
                    ControllerEmail = badEmail ? "" : "a@b.test",
                    DataProtectionOfficer = new DataProtectionOfficer(badDpoName ? "" : "Dana", badDpoEmail ? " " : "d@b.test")
                });
                var expected = new[] { badName, badEmail, badDpoName, badDpoEmail }.Count(b => b);
                return expected == 0
                    ? result.Succeeded
                    : result.Failed && result.Failures!.Count() == expected;
            });

    // -- DefaultGDPRComplianceValidator --

    [Property(MaxTest = 50)]
    public bool DefaultValidator_AnyRequest_YieldsCompliantResultWithoutMessages(NonEmptyString request)
    {
        var sut = new DefaultGDPRComplianceValidator();

        var result = sut.ValidateAsync(request.Get, RequestContext.CreateForTest(), CancellationToken.None).AsTask().Result;

        return result.Match(
            Right: r => r.IsCompliant && r.Errors.Count == 0 && r.Warnings.Count == 0,
            Left: _ => false);
    }

    [Property(MaxTest = 20)]
    public bool DefaultValidator_AnyRequestType_NeverReturnsLeft(int request)
    {
        var sut = new DefaultGDPRComplianceValidator();

        var result = sut.ValidateAsync(request, RequestContext.CreateForTest(), CancellationToken.None).AsTask().Result;

        return result.IsRight;
    }
}
