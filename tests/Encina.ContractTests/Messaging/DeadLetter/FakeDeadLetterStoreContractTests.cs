using Encina.Messaging.DeadLetter;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;

namespace Encina.ContractTests.Messaging.DeadLetter;

/// <summary>
/// Runs <see cref="DeadLetterStoreContract"/> against <see cref="FakeDeadLetterStore"/>, so the fake keeps
/// the behavior the persistent stores are held to.
/// </summary>
public sealed class FakeDeadLetterStoreContractTests : DeadLetterStoreContract
{
    private FakeDeadLetterStore _store = null!;

    protected override Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        _store = new FakeDeadLetterStore(timeProvider);
        return Task.FromResult<IDeadLetterStore>(_store);
    }

    // The fake is one in-memory queue: the second "store" is the same instance.
    protected override IDeadLetterStore CreateSecondStore() => _store;

    protected override IDeadLetterMessage CreateMessage(DeadLetterData data) => new FakeDeadLetterMessage
    {
        Id = data.Id,
        RequestType = data.RequestType,
        RequestContent = data.RequestContent,
        ErrorCode = data.ErrorCode,
        ExceptionType = data.ExceptionType,
        ExceptionStackTrace = data.ExceptionStackTrace,
        CorrelationId = data.CorrelationId,
        SourcePattern = data.SourcePattern,
        SourceMessageId = data.SourceMessageId,
        TenantId = data.TenantId,
        TotalRetryAttempts = data.TotalRetryAttempts,
        FirstFailedAtUtc = data.FirstFailedAtUtc,
        DeadLetteredAtUtc = data.DeadLetteredAtUtc,
        ExpiresAtUtc = data.ExpiresAtUtc
    };
}
