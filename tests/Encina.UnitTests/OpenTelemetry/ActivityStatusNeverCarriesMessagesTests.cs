using Encina.Caching;
using Encina.Diagnostics;
using Encina.DomainModeling;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.Audit;
using Encina.OpenTelemetry.MessagingStores;
using Encina.OpenTelemetry.QueryCache;
using Encina.OpenTelemetry.UnitOfWork;
using Encina.Security.Audit;
using LanguageExt;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Proves, per decorator family, that a failure writes only the error code or the exception type to
/// the activity status description and tags, never the message (AGENTS.md section 3, #2134).
/// </summary>
public sealed class ActivityStatusNeverCarriesMessagesTests
{
    private const string Code = "test.sensitive_failure";

    private static (EncinaError Error, string Secret) NewError()
    {
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        return (EncinaErrors.Create(Code, "value " + secret), secret);
    }

    private static ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> ThrowingAuditQuery(string secret)
        => throw new InvalidOperationException(secret);

    [Fact]
    public async Task AuditStore_WhenInnerReturnsError_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var inner = Substitute.For<IOperationAuditStore>();
        inner.GetByEntityAsync("Order", "1", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<OperationAuditEntry>>.Left(error));

        await new InstrumentedOperationAuditStore(inner).GetByEntityAsync("Order", "1");

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public async Task AuditStore_WhenInnerThrows_StatusIsExceptionType()
    {
        using var capture = new ActivityStatusCapture();
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var inner = Substitute.For<IOperationAuditStore>();
#pragma warning disable CA2012 // NSubstitute configuration call, the ValueTask is never consumed
        inner.GetByEntityAsync("Order", "1", Arg.Any<CancellationToken>())
            .Returns(_ => ThrowingAuditQuery(secret));
#pragma warning restore CA2012

        var act = async () => await new InstrumentedOperationAuditStore(inner).GetByEntityAsync("Order", "1");

        await Should.ThrowAsync<InvalidOperationException>(act);
        capture.ShouldHaveErrorStatus(nameof(InvalidOperationException), secret);
    }

    [Fact]
    public async Task UnitOfWork_WhenInnerReturnsError_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var inner = Substitute.For<IUnitOfWork>();
        inner.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Either<EncinaError, int>.Left(error));
        var sut = new InstrumentedUnitOfWork(inner);

        await sut.SaveChangesAsync();

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public async Task CacheProvider_WhenInnerThrows_StatusIsExceptionType()
    {
        using var capture = new ActivityStatusCapture();
        var secret = "SECRET-" + Guid.NewGuid().ToString("N");
        var inner = Substitute.For<ICacheProvider>();
        inner.RemoveAsync("k", Arg.Any<CancellationToken>()).Throws(new TimeoutException(secret));
        var sut = new InstrumentedCacheProvider(inner);

        var act = async () => await sut.RemoveAsync("k", CancellationToken.None);

        await Should.ThrowAsync<TimeoutException>(act);
        capture.ShouldHaveErrorStatus(nameof(TimeoutException), secret);
    }

    [Fact]
    public async Task OutboxStore_WhenInnerReturnsError_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var id = Guid.NewGuid();
        var inner = Substitute.For<IOutboxStore>();
        inner.MarkAsProcessedAsync(id, Arg.Any<CancellationToken>()).Returns(Either<EncinaError, Unit>.Left(error));

        await new InstrumentedOutboxStore(inner).MarkAsProcessedAsync(id);

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public async Task SagaStore_WhenInnerReturnsError_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var id = Guid.NewGuid();
        var inner = Substitute.For<ISagaStore>();
        inner.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Option<ISagaState>>.Left(error));

        await new InstrumentedSagaStore(inner).GetAsync(id);

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public async Task ScheduledMessageStore_WhenInnerReturnsError_StatusIsErrorCode()
    {
        using var capture = new ActivityStatusCapture();
        var (error, secret) = NewError();
        var id = Guid.NewGuid();
        var inner = Substitute.For<IScheduledMessageStore>();
        inner.MarkAsProcessedAsync(id, Arg.Any<CancellationToken>()).Returns(Either<EncinaError, Unit>.Left(error));

        await new InstrumentedScheduledMessageStore(inner).MarkAsProcessedAsync(id);

        capture.ShouldHaveErrorStatus(Code, secret);
    }

    [Fact]
    public void ForError_WithoutCode_ReturnsFallbackNotMessage()
    {
        var error = EncinaError.New("raw message with secret");

        ActivityStatusDescriptions.ForError(error).ShouldBe("Encina.unknown");
    }

    [Fact]
    public void ForException_ReturnsTypeNameOnly()
    {
        ActivityStatusDescriptions.ForException(new ArgumentException("secret"))
            .ShouldBe(nameof(ArgumentException));
    }

    [Fact]
    public void ForException_Null_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ActivityStatusDescriptions.ForException(null!));
    }
}
