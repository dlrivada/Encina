using System.Diagnostics;

using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis;
using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Shouldly;

using static LanguageExt.Prelude;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

#pragma warning disable CA2012 // NSubstitute Returns with ValueTask

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Pipeline;

/// <summary>
/// The data subject's own identifier and the raw <see cref="EncinaError.Message"/> must never
/// reach a log message or an OpenTelemetry activity tag produced by
/// <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/>'s consent-check and
/// enforcement paths (#1435, following #1314/#1426). Modelled on
/// <c>Encina.UnitTests.Compliance.Consent.ConsentPiiLeakTests</c>.
/// </summary>
public sealed class LawfulBasisSubjectIdLeakTests
{
    private const string SubjectId = "patient-42";
    private const string SecretProviderErrorMessage = "SECRET-PROVIDER-ERROR-MESSAGE-DO-NOT-LOG";
    private const string SecretLiaErrorMessage = "SECRET-LIA-ERROR-MESSAGE-DO-NOT-LOG";

    [LawfulBasis(GDPRLawfulBasis.Consent, Purpose = "Marketing")]
    public sealed record ConsentRequest : IRequest<string>;

    [LawfulBasis(GDPRLawfulBasis.LegitimateInterests, Purpose = "Fraud", LIAReference = "LIA-001")]
    public sealed record LIRequest : IRequest<string>;

    private static RequestHandlerCallback<string> SuccessCallback(string value = "ok") =>
        () => new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>(value));

    [Fact]
    public async Task ConsentCheck_ValidConsent_NeverCarriesSubjectId()
    {
        var subjectIdExtractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        subjectIdExtractor.ExtractSubjectId(Arg.Any<ConsentRequest>(), Arg.Any<IRequestContext>()).Returns(SubjectId);

        var consentProvider = Substitute.For<IConsentStatusProvider>();
        consentProvider.CheckConsentAsync(SubjectId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ConsentCheckResult>>(
                Right<EncinaError, ConsentCheckResult>(new ConsentCheckResult(true, []))));

        var (behavior, logger) = CreateBehavior<ConsentRequest>(subjectIdExtractor, consentProvider);
        using var capture = new DiagnosticsCapture();

        var result = await behavior.Handle(
            new ConsentRequest(), Substitute.For<IRequestContext>(), SuccessCallback("consent-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSecret(logger, SubjectId);
    }

    [Fact]
    public async Task ConsentCheck_NoActiveConsent_NeverCarriesSubjectId()
    {
        var subjectIdExtractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        subjectIdExtractor.ExtractSubjectId(Arg.Any<ConsentRequest>(), Arg.Any<IRequestContext>()).Returns(SubjectId);

        var consentProvider = Substitute.For<IConsentStatusProvider>();
        consentProvider.CheckConsentAsync(SubjectId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ConsentCheckResult>>(
                Right<EncinaError, ConsentCheckResult>(new ConsentCheckResult(false, ["Marketing"]))));

        var (behavior, logger) = CreateBehavior<ConsentRequest>(subjectIdExtractor, consentProvider);
        using var capture = new DiagnosticsCapture();

        var result = await behavior.Handle(
            new ConsentRequest(), Substitute.For<IRequestContext>(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSecret(logger, SubjectId);
    }

    [Fact]
    public async Task ConsentCheck_ProviderError_NeverCarriesSubjectIdOrRawErrorMessage()
    {
        var subjectIdExtractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        subjectIdExtractor.ExtractSubjectId(Arg.Any<ConsentRequest>(), Arg.Any<IRequestContext>()).Returns(SubjectId);

        var providerError = EncinaErrors.Create("consent.provider_error", SecretProviderErrorMessage);
        var consentProvider = Substitute.For<IConsentStatusProvider>();
        consentProvider.CheckConsentAsync(SubjectId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ConsentCheckResult>>(
                Left<EncinaError, ConsentCheckResult>(providerError)));

        var (behavior, logger) = CreateBehavior<ConsentRequest>(subjectIdExtractor, consentProvider);
        using var capture = new DiagnosticsCapture();

        var result = await behavior.Handle(
            new ConsentRequest(), Substitute.For<IRequestContext>(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSecret(logger, SubjectId);
        capture.AssertNoSecret(logger, SecretProviderErrorMessage);

        // The error code is the expected, non-identifying correlation the log line carries instead.
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("consent.provider_error", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConsentCheck_SubjectIdUnresolved_NeverCarriesSubjectId()
    {
        var subjectIdExtractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        subjectIdExtractor.ExtractSubjectId(Arg.Any<ConsentRequest>(), Arg.Any<IRequestContext>()).Returns((string?)null);

        var (behavior, logger) = CreateBehavior<ConsentRequest>(subjectIdExtractor, Substitute.For<IConsentStatusProvider>());
        using var capture = new DiagnosticsCapture();

        var result = await behavior.Handle(
            new ConsentRequest(), Substitute.For<IRequestContext>(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSecret(logger, SubjectId);
    }

    [Fact]
    public async Task LIACheck_ServiceError_NeverCarriesRawErrorMessage()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var liaError = EncinaErrors.Create("lia.service_error", SecretLiaErrorMessage);
        service.HasApprovedLIAAsync("LIA-001", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Left<EncinaError, bool>(liaError)));

        var (behavior, logger) = CreateBehavior<LIRequest>(
            Substitute.For<ILawfulBasisSubjectIdExtractor>(), Substitute.For<IConsentStatusProvider>(), service: service);
        using var capture = new DiagnosticsCapture();

        var result = await behavior.Handle(
            new LIRequest(), Substitute.For<IRequestContext>(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSecret(logger, SecretLiaErrorMessage);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("lia.service_error", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LIACheck_ServiceError_WarnMode_NeverCarriesRawErrorMessage()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var liaError = EncinaErrors.Create("lia.service_error", SecretLiaErrorMessage);
        service.HasApprovedLIAAsync("LIA-001", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Left<EncinaError, bool>(liaError)));

        var (behavior, logger) = CreateBehavior<LIRequest>(
            Substitute.For<ILawfulBasisSubjectIdExtractor>(), Substitute.For<IConsentStatusProvider>(), service: service,
            options: new LawfulBasisOptions { EnforcementMode = LawfulBasisEnforcementMode.Warn });
        using var capture = new DiagnosticsCapture();

        var result = await behavior.Handle(
            new LIRequest(), Substitute.For<IRequestContext>(), SuccessCallback("warn-ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSecret(logger, SecretLiaErrorMessage);

        // Warn mode logs EnforcementWarning with the error code, not the raw message.
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("lia.service_error", StringComparison.Ordinal));
    }

    private static (LawfulBasisValidationPipelineBehavior<TRequest, string> Behavior, FakeLogger<LawfulBasisValidationPipelineBehavior<TRequest, string>> Logger) CreateBehavior<TRequest>(
        ILawfulBasisSubjectIdExtractor subjectIdExtractor,
        IConsentStatusProvider? consentProvider,
        ILawfulBasisService? service = null,
        LawfulBasisOptions? options = null)
        where TRequest : IRequest<string>
    {
        var logger = new FakeLogger<LawfulBasisValidationPipelineBehavior<TRequest, string>>();
        var behavior = new LawfulBasisValidationPipelineBehavior<TRequest, string>(
            service ?? Substitute.For<ILawfulBasisService>(),
            subjectIdExtractor,
            Options.Create(options ?? new LawfulBasisOptions()),
            logger,
            consentProvider);
        return (behavior, logger);
    }

    /// <summary>
    /// Captures every log message and activity tag/status recorded on the
    /// <c>Encina.Compliance.LawfulBasis</c> ActivitySource for the duration of the scope, so a
    /// test can assert a secret value never appears in them.
    /// </summary>
    private sealed class DiagnosticsCapture : IDisposable
    {
        private readonly ActivityListener _activityListener;
        private readonly List<Activity> _activities = [];

        public DiagnosticsCapture()
        {
            _activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == LawfulBasisDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => _activities.Add(activity)
            };
            ActivitySource.AddActivityListener(_activityListener);
        }

        public void AssertNoSecret(FakeLogger logger, string secret)
        {
            var logs = logger.Collector.GetSnapshot();
            logs.ShouldAllBe(r => !r.Message.Contains(secret, StringComparison.Ordinal));

            foreach (var activity in _activities.ToArray())
            {
                activity.DisplayName.ShouldNotContain(secret);
                (activity.StatusDescription ?? string.Empty).ShouldNotContain(secret);
                foreach (var tag in activity.Tags)
                {
                    (tag.Value ?? string.Empty).ShouldNotContain(secret);
                }
            }
        }

        public void Dispose() => _activityListener.Dispose();
    }
}
#pragma warning restore CA2012
