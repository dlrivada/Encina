using Encina.Hangfire;
using global::Hangfire;
using global::Hangfire.Common;
using Shouldly;

namespace Encina.UnitTests.Hangfire;

/// <summary>
/// Unit tests for <see cref="EncinaAutomaticRetry"/> and the job failure exceptions.
/// </summary>
public sealed class EncinaAutomaticRetryTests
{
    [Fact]
    public void Create_ExcludesPermanentFailuresFromRetries()
    {
        var filter = EncinaAutomaticRetry.Create(attempts: 3);

        filter.Attempts.ShouldBe(3);
        filter.ExceptOn.ShouldBe([typeof(EncinaJobPermanentFailureException)]);
        filter.OnAttemptsExceeded.ShouldBe(AttemptsExceededAction.Fail);
    }

    [Fact]
    public void Create_DefaultAttempts_MatchesHangfireDefault()
    {
        EncinaAutomaticRetry.Create().Attempts.ShouldBe(AutomaticRetryAttribute.DefaultRetryAttempts);
        EncinaAutomaticRetry.DefaultAttempts.ShouldBe(AutomaticRetryAttribute.DefaultRetryAttempts);
    }

    [Fact]
    public void Create_NegativeAttempts_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => EncinaAutomaticRetry.Create(-1));
    }

    [Fact]
    public void UseEncinaAutomaticRetry_ReplacesExistingRetryFilters()
    {
        var filters = new JobFilterCollection
        {
            new AutomaticRetryAttribute { Attempts = 10 }
        };
        var unrelated = new DisableConcurrentExecutionAttribute(60);
        filters.Add(unrelated);

        var returned = filters.UseEncinaAutomaticRetry(attempts: 4);

        returned.ShouldBeSameAs(filters);
        var retryFilters = filters.Select(f => f.Instance).OfType<AutomaticRetryAttribute>().ToList();
        retryFilters.Count.ShouldBe(1);
        retryFilters[0].Attempts.ShouldBe(4);
        retryFilters[0].ExceptOn.ShouldContain(typeof(EncinaJobPermanentFailureException));
        filters.Select(f => f.Instance).ShouldContain(unrelated);
    }

    [Fact]
    public void UseEncinaAutomaticRetry_NullFilters_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ((JobFilterCollection)null!).UseEncinaAutomaticRetry());
    }

    [Fact]
    public void PermanentFailureException_FromError_CarriesCodeAndSanitizedInnerException()
    {
        var cause = new InvalidOperationException("boom for patient-1");
        var error = EncinaErrors.Create("consent.missing", "Consent missing for subject 'patient-1'", cause);

        var exception = new EncinaJobPermanentFailureException(error);

        exception.ErrorCode.ShouldBe("consent.missing");
        // Hangfire persists the full exception chain, so the cause's own Message (which may
        // carry personal data) never becomes the InnerException's message (#1259 review).
        exception.InnerException.ShouldNotBeSameAs(cause);
        exception.InnerException!.Message.ShouldContain(nameof(InvalidOperationException));
        exception.InnerException!.Message.ShouldNotContain("patient-1");
        exception.Message.ShouldNotContain("patient-1");
        exception.Data[EncinaJobPermanentFailureException.ErrorCodeDataKey].ShouldBe("consent.missing");
    }

    [Fact]
    public void FailureExceptions_StandardConstructors_UseUnknownCode()
    {
        new EncinaJobFailedException().ErrorCode.ShouldBe("encina.unknown");
        new EncinaJobFailedException("m").Message.ShouldBe("m");
        new EncinaJobFailedException("m", new InvalidOperationException()).InnerException.ShouldNotBeNull();
        new EncinaJobPermanentFailureException().ErrorCode.ShouldBe("encina.unknown");
        new EncinaJobPermanentFailureException("m").Message.ShouldBe("m");
        new EncinaJobPermanentFailureException("m", new InvalidOperationException()).InnerException.ShouldNotBeNull();
    }
}
