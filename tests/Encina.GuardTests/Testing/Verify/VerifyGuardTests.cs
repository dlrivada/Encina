using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Verify;

using LanguageExt;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Testing.Verify;

/// <summary>
/// Guard tests for Encina.Testing.Verify covering ThrowIfNull guards on
/// <see cref="EncinaVerify"/> static methods.
/// </summary>
[Trait("Category", "Guard")]
public sealed class VerifyGuardTests
{
    // ─── PrepareUncommittedEvents ───

    [Fact]
    public void PrepareUncommittedEvents_NullAggregate_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaVerify.PrepareUncommittedEvents(null!));
    }

    // ─── PrepareOutboxMessages ───

    [Fact]
    public void PrepareOutboxMessages_NullMessages_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaVerify.PrepareOutboxMessages(null!));
    }

    [Fact]
    public void PrepareOutboxMessages_EmptyCollection_ReturnsEmpty()
    {
        var result = EncinaVerify.PrepareOutboxMessages([]);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    // ─── PrepareInboxMessages ───

    [Fact]
    public void PrepareInboxMessages_NullMessages_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaVerify.PrepareInboxMessages(null!));
    }

    [Fact]
    public void PrepareInboxMessages_EmptyCollection_ReturnsEmpty()
    {
        var result = EncinaVerify.PrepareInboxMessages([]);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    // ─── PrepareSagaState ───

    [Fact]
    public void PrepareSagaState_NullSagaState_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaVerify.PrepareSagaState(null!));
    }

    // ─── PrepareScheduledMessages ───

    [Fact]
    public void PrepareScheduledMessages_NullMessages_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaVerify.PrepareScheduledMessages(null!));
    }

    [Fact]
    public void PrepareScheduledMessages_EmptyCollection_ReturnsEmpty()
    {
        var result = EncinaVerify.PrepareScheduledMessages([]);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    // ─── PrepareDeadLetterMessages ───

    [Fact]
    public void PrepareDeadLetterMessages_NullMessages_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaVerify.PrepareDeadLetterMessages(null!));
    }

    [Fact]
    public void PrepareDeadLetterMessages_EmptyCollection_ReturnsEmpty()
    {
        var result = EncinaVerify.PrepareDeadLetterMessages([]);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    // ─── PrepareHandlerResult ───

    [Fact]
    public void PrepareHandlerResult_NullRequest_Throws()
    {
        var result = Either<EncinaError, string>.Right("ok");
        Should.Throw<ArgumentNullException>(() =>
            EncinaVerify.PrepareHandlerResult<object, string>(null!, result));
    }

    // ─── PrepareSagaStates ───

    [Fact]
    public void PrepareSagaStates_NullSagaStates_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaVerify.PrepareSagaStates(null!));
    }

    // ─── PrepareValidationError (EncinaError is a struct — no null guard, tested via happy path) ───

    // ─── Happy paths for non-null methods ───

    [Fact]
    public void PrepareEither_Right_ReturnsObject()
    {
        var either = Either<EncinaError, string>.Right("hello");
        var result = EncinaVerify.PrepareEither(either);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void PrepareEither_Left_ReturnsObject()
    {
        var either = Either<EncinaError, string>.Left(EncinaError.New("err"));
        var result = EncinaVerify.PrepareEither(either);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ExtractSuccess_Right_ReturnsValue()
    {
        var result = Either<EncinaError, string>.Right("value");
        var extracted = EncinaVerify.ExtractSuccess(result);
        extracted.ShouldBe("value");
    }

    [Fact]
    public void ExtractError_Left_ReturnsError()
    {
        var result = Either<EncinaError, string>.Left(EncinaError.New("fail"));
        var error = EncinaVerify.ExtractError(result);
        error.Message.ShouldBe("fail");
    }

    [Fact]
    public void PrepareHandlerResult_ValidArgs_ReturnsObject()
    {
        var request = new { Id = 1 };
        var result = Either<EncinaError, string>.Right("ok");
        var prepared = EncinaVerify.PrepareHandlerResult(request, result);
        prepared.ShouldNotBeNull();
    }

    [Fact]
    public void PrepareValidationError_ValidError_ReturnsObject()
    {
        var error = EncinaErrors.Create("encina.validation.test", "validation failed");
        var prepared = EncinaVerify.PrepareValidationError(error);
        prepared.ShouldNotBeNull();
    }

    // ─── EncinaVerifySettings ───

    [Fact]
    public void EncinaVerifySettings_Type_Exists()
    {
        typeof(EncinaVerifySettings).ShouldNotBeNull();
    }
}
