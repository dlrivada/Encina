#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Encryption;

/// <summary>
/// The pipeline failure log carries the error code, never the error message (#1557).
/// </summary>
public sealed class EncryptionPipelineBehaviorRedactionTests : IDisposable
{
    private const string Sentinel = "SENTINEL-encryption-error-message";
    private const string Code = "encryption.sentinel.code";

    private readonly IEncryptionOrchestrator _orchestrator = Substitute.For<IEncryptionOrchestrator>();
    private readonly IRequestContext _context = RequestContext.CreateForTest(userId: "user-1");

    public EncryptionPipelineBehaviorRedactionTests() => EncryptedPropertyCache.ClearCache();

    public void Dispose() => EncryptedPropertyCache.ClearCache();

    private static EncinaError SentinelError() => EncinaErrors.Create(Code, Sentinel);

    private static void AssertRedacted(FakeLogger<EncryptionPipelineBehavior<TRequestMarker, Unit>> logger)
    {
        var warnings = logger.Collector.GetSnapshot().Where(r => r.Level == Microsoft.Extensions.Logging.LogLevel.Warning).ToList();
        warnings.ShouldNotBeEmpty();
        warnings.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
        warnings.ShouldContain(r => r.Message.Contains(Code, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_DecryptFails_LogsTheErrorCodeNotTheMessage()
    {
        var logger = new FakeLogger<EncryptionPipelineBehavior<TRequestMarker, Unit>>();
        var sut = new EncryptionPipelineBehavior<TRequestMarker, Unit>(_orchestrator, Options.Create(new EncryptionOptions()), logger);
        _orchestrator.DecryptAsync(Arg.Any<TRequestMarker>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TRequestMarker>>(Left(SentinelError())));

        var result = await sut.Handle(new TRequestMarker(), _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        AssertRedacted(logger);
    }

    [Fact]
    public async Task Handle_EncryptFails_LogsTheErrorCodeNotTheMessage()
    {
        var logger = new FakeLogger<EncryptionPipelineBehavior<TRequestMarker, Unit>>();
        var sut = new EncryptionPipelineBehavior<TRequestMarker, Unit>(_orchestrator, Options.Create(new EncryptionOptions()), logger);
        _orchestrator.DecryptAsync(Arg.Any<TRequestMarker>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TRequestMarker>>(Right(new TRequestMarker())));
        _orchestrator.EncryptAsync(Arg.Any<TRequestMarker>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TRequestMarker>>(Left(SentinelError())));

        var result = await sut.Handle(new TRequestMarker(), _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        AssertRedacted(logger);
    }

    [Fact]
    public async Task Handle_ResponseEncryptFails_LogsTheErrorCodeNotTheMessage()
    {
        var logger = new FakeLogger<EncryptionPipelineBehavior<ResponseRequest, EncryptedResponseDto>>();
        var sut = new EncryptionPipelineBehavior<ResponseRequest, EncryptedResponseDto>(
            _orchestrator, Options.Create(new EncryptionOptions()), logger);
        _orchestrator.EncryptAsync(Arg.Any<EncryptedResponseDto>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, EncryptedResponseDto>>(Left(SentinelError())));

        await sut.Handle(new ResponseRequest(), _context,
            () => ValueTask.FromResult<Either<EncinaError, EncryptedResponseDto>>(Right(new EncryptedResponseDto())),
            CancellationToken.None);

        var warnings = logger.Collector.GetSnapshot().Where(r => r.Level == Microsoft.Extensions.Logging.LogLevel.Warning).ToList();
        warnings.ShouldNotBeEmpty();
        warnings.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
        warnings.ShouldContain(r => r.Message.Contains(Code, StringComparison.Ordinal));
    }

    [DecryptOnReceive]
    public sealed class TRequestMarker : ICommand<Unit>
    {
        [Encrypt(Purpose = "Payload")]
        public string Data { get; set; } = string.Empty;
    }

    public sealed class ResponseRequest : IQuery<EncryptedResponseDto> { }

    [EncryptedResponse]
    public sealed class EncryptedResponseDto
    {
        public string Value { get; set; } = string.Empty;
    }
}
