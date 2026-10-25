using System.Net;
using System.Text;

using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Encina.UnitTests.Compliance.Attestation;

[Trait("Category", "Unit")]
[Trait("Feature", "Attestation")]
public sealed class HttpAttestationProviderTests
{
    private static readonly Uri AttestUrl = new("https://attest.example.com/attest");
    private static readonly Uri VerifyUrl = new("https://attest.example.com/verify");

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request, body));
            return await responder(request, cancellationToken);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private HttpAttestationProvider Create(StubHandler handler, Action<HttpAttestationOptions>? configure = null)
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = AttestUrl, VerifyEndpointUrl = VerifyUrl };
        configure?.Invoke(options);
        return new HttpAttestationProvider(
            new HttpClient(handler),
            _time,
            NullLogger<HttpAttestationProvider>.Instance,
            Options.Create(options));
    }

    private static StubHandler Respond(HttpStatusCode status, string json) =>
        new((_, _) => Task.FromResult(Json(status, json)));

    private static StubHandler Throw(Exception exception) =>
        new((_, _) => throw exception);

    private static AuditRecord Record() => new()
    {
        RecordId = Guid.NewGuid(),
        RecordType = "Decision",
        OccurredAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        SerializedContent = "{\"a\":1}",
        CorrelationId = "corr-1"
    };

    private static AttestationReceipt Receipt() => new()
    {
        AttestationId = Guid.NewGuid(),
        AuditRecordId = Guid.NewGuid(),
        ContentHash = "hash",
        Signature = "sig",
        ProviderName = "Http",
        AttestedAtUtc = DateTimeOffset.UnixEpoch
    };

    private static string AttestResponse(Guid id, string signature = "ext-sig", string extra = "") =>
        $"{{\"attestation_id\":\"{id}\",\"signature\":\"{signature}\"{extra}}}";

    private static T Right<T>(LanguageExt.Either<EncinaError, T> either) =>
        either.Match(r => r, l => throw new InvalidOperationException("Expected Right but got " + l.GetCode()));

    private static string LeftCode<T>(LanguageExt.Either<EncinaError, T> either) =>
        either.Match(_ => throw new InvalidOperationException("Expected Left"), l => l.GetCode().IfNone("none"));

    #region AttestAsync

    [Fact]
    public void ProviderName_IsHttp() =>
        Create(Respond(HttpStatusCode.OK, "{}")).ProviderName.ShouldBe("Http");

    [Fact]
    public async Task AttestAsync_SuccessfulResponse_ReturnsReceiptWithExternalIdentifierAndSignature()
    {
        var externalId = Guid.NewGuid();
        var handler = Respond(HttpStatusCode.OK, AttestResponse(externalId, "ext-sig"));
        var sut = Create(handler);
        var record = Record();

        var receipt = Right(await sut.AttestAsync(record));

        receipt.AttestationId.ShouldBe(externalId);
        receipt.AuditRecordId.ShouldBe(record.RecordId);
        receipt.Signature.ShouldBe("ext-sig");
        receipt.ProviderName.ShouldBe("Http");
        receipt.CorrelationId.ShouldBe("corr-1");
        receipt.AttestedAtUtc.ShouldBe(_time.GetUtcNow());
        receipt.ContentHash.ShouldBe(AttestationHasher.ComputeContentHash(record));
        receipt.ProofMetadata!["transport"].ShouldBe("http");
    }

    [Fact]
    public async Task AttestAsync_SendsContentHashAndRecordIdentityToConfiguredEndpoint()
    {
        var handler = Respond(HttpStatusCode.OK, AttestResponse(Guid.NewGuid()));
        var sut = Create(handler);
        var record = Record();

        await sut.AttestAsync(record);

        var (request, body) = handler.Calls.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri.ShouldBe(AttestUrl);
        body.ShouldContain(record.RecordId.ToString());
        body.ShouldContain(AttestationHasher.ComputeContentHash(record));
        body.ShouldNotContain(record.SerializedContent, Case.Sensitive);
    }

    [Fact]
    public async Task AttestAsync_AuthHeaderConfigured_SendsAuthorizationHeader()
    {
        var handler = Respond(HttpStatusCode.OK, AttestResponse(Guid.NewGuid()));
        var sut = Create(handler, o => o.AuthHeader = "Bearer abc");

        await sut.AttestAsync(Record());

        handler.Calls.Single().Request.Headers.GetValues("Authorization").Single().ShouldBe("Bearer abc");
    }

    [Fact]
    public async Task AttestAsync_NoAuthHeader_OmitsAuthorizationHeader()
    {
        var handler = Respond(HttpStatusCode.OK, AttestResponse(Guid.NewGuid()));
        var sut = Create(handler);

        await sut.AttestAsync(Record());

        handler.Calls.Single().Request.Headers.Contains("Authorization").ShouldBeFalse();
    }

    [Fact]
    public async Task AttestAsync_ProofMetadataInResponse_CopiesEntriesAsStrings()
    {
        var extra = ",\"proof_metadata\":{\"log_index\":42,\"ledger\":\"rekor\"}";
        var sut = Create(Respond(HttpStatusCode.OK, AttestResponse(Guid.NewGuid(), extra: extra)));

        var receipt = Right(await sut.AttestAsync(Record()));

        receipt.ProofMetadata!["log_index"].ShouldBe("42");
        receipt.ProofMetadata["ledger"].ShouldBe("rekor");
        receipt.ProofMetadata["transport"].ShouldBe("http");
    }

    [Fact]
    public async Task AttestAsync_ProofMetadataNotAnObject_IgnoresItAndKeepsTransportOnly()
    {
        var extra = ",\"proof_metadata\":\"nope\"";
        var sut = Create(Respond(HttpStatusCode.OK, AttestResponse(Guid.NewGuid(), extra: extra)));

        var receipt = Right(await sut.AttestAsync(Record()));

        receipt.ProofMetadata!.Count.ShouldBe(1);
        receipt.ProofMetadata.ContainsKey("transport").ShouldBeTrue();
    }

    [Fact]
    public async Task AttestAsync_NonSuccessStatus_ReturnsHttpEndpointErrorWithStatusAndExcerpt()
    {
        var sut = Create(Respond(HttpStatusCode.InternalServerError, "server exploded"));

        var result = await sut.AttestAsync(Record());

        var error = result.Match(_ => throw new InvalidOperationException(), l => l);
        error.GetCode().IfNone("").ShouldBe(AttestationErrors.HttpEndpointErrorCode);
        error.GetDetails()["statusCode"].ShouldBe(500);
        error.GetDetails()["responseExcerpt"].ShouldBe("server exploded");
    }

    [Fact]
    public async Task AttestAsync_NonSuccessWithLongBody_TruncatesExcerptTo500CharactersPlusEllipsis()
    {
        var sut = Create(Respond(HttpStatusCode.BadGateway, new string('x', 800)));

        var result = await sut.AttestAsync(Record());

        var error = result.Match(_ => throw new InvalidOperationException(), l => l);
        var excerpt = (string)error.GetDetails()["responseExcerpt"]!;
        excerpt.Length.ShouldBe(501);
        excerpt.ShouldEndWith("…");
    }

    [Theory]
    [InlineData("{\"signature\":\"s\"}")]
    [InlineData("{\"attestation_id\":\"not-a-guid\",\"signature\":\"s\"}")]
    public async Task AttestAsync_MissingOrInvalidAttestationId_ReturnsProviderUnavailable(string json)
    {
        var sut = Create(Respond(HttpStatusCode.OK, json));

        var result = await sut.AttestAsync(Record());

        LeftCode(result).ShouldBe(AttestationErrors.ProviderUnavailableCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(",\"signature\":\"   \"")]
    public async Task AttestAsync_MissingOrBlankSignature_ReturnsProviderUnavailable(string signatureFragment)
    {
        var json = $"{{\"attestation_id\":\"{Guid.NewGuid()}\"{signatureFragment}}}";
        var sut = Create(Respond(HttpStatusCode.OK, json));

        var result = await sut.AttestAsync(Record());

        LeftCode(result).ShouldBe(AttestationErrors.ProviderUnavailableCode);
    }

    private const string Sentinel = "SENTINEL-payload-text";

    private static HttpResponseMessage Raw(string body, string mediaType) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    private static StubHandler RespondRaw(string body, string mediaType = "application/json") =>
        new((_, _) => Task.FromResult(Raw(body, mediaType)));

    private static EncinaError LeftError<T>(LanguageExt.Either<EncinaError, T> either) =>
        either.Match(_ => throw new InvalidOperationException("Expected Left"), l => l);

    [Fact]
    public async Task AttestAsync_EmptyBody_ReturnsProviderUnavailableAndDoesNotThrow()
    {
        var sut = Create(RespondRaw(""));

        var result = await sut.AttestAsync(Record());

        LeftCode(result).ShouldBe(AttestationErrors.ProviderUnavailableCode);
    }

    [Theory]
    [InlineData("not json", "application/json")]
    [InlineData("plain text", "text/plain")]
    [InlineData("<html>gateway error</html>", "text/html")]
    [InlineData("[1,2,3]", "application/json")]
    [InlineData("\"just a string\"", "application/json")]
    public async Task AttestAsync_MalformedSuccessBody_ReturnsProviderUnavailable(string body, string mediaType)
    {
        var sut = Create(RespondRaw(body, mediaType));

        var result = await sut.AttestAsync(Record());

        LeftCode(result).ShouldBe(AttestationErrors.ProviderUnavailableCode);
    }

    [Theory]
    [InlineData("{\"attestation_id\":123,\"signature\":\"s\"}")]
    [InlineData("{\"attestation_id\":true,\"signature\":\"s\"}")]
    [InlineData("{\"attestation_id\":\"00000000-0000-0000-0000-000000000001\",\"signature\":42}")]
    [InlineData("{\"attestation_id\":\"00000000-0000-0000-0000-000000000001\",\"signature\":{\"a\":1}}")]
    [InlineData("{\"attestation_id\":null,\"signature\":\"s\"}")]
    [InlineData("{\"attestation_id\":\"00000000-0000-0000-0000-000000000001\",\"signature\":null}")]
    public async Task AttestAsync_WrongTypedOrNullFields_ReturnsProviderUnavailable(string json)
    {
        var sut = Create(Respond(HttpStatusCode.OK, json));

        var result = await sut.AttestAsync(Record());

        LeftCode(result).ShouldBe(AttestationErrors.ProviderUnavailableCode);
    }

    [Fact]
    public async Task AttestAsync_MalformedBody_LeftMessageNamesExceptionTypeButNeverItsMessageOrPayload()
    {
        var sut = Create(RespondRaw(Sentinel));

        var error = LeftError(await sut.AttestAsync(Record()));

        error.Message.ShouldContain("JsonException");
        error.Message.ShouldNotContain(Sentinel);
        (error.GetDetails()["reason"]?.ToString() ?? "").ShouldNotContain(Sentinel);
    }

    [Fact]
    public async Task AttestAsync_UnknownCharset_LeftMessageNamesInvalidOperationException()
    {
        var sut = Create(new StubHandler((_, _) => Task.FromResult(UnsupportedCharset())));

        var error = LeftError(await sut.AttestAsync(Record()));

        error.Message.ShouldContain("InvalidOperationException");
        error.Message.ShouldNotContain(Sentinel);
    }

    [Fact]
    public async Task VerifyAsync_UnknownCharset_ReturnsProviderUnavailable()
    {
        var sut = Create(new StubHandler((_, _) => Task.FromResult(UnsupportedCharset())));

        var error = LeftError(await sut.VerifyAsync(Receipt()));

        error.GetCode().IfNone("").ShouldBe(AttestationErrors.ProviderUnavailableCode);
        error.Message.ShouldContain("InvalidOperationException");
    }

    private static HttpResponseMessage UnsupportedCharset() =>
        new(HttpStatusCode.OK)
        {
            Content = BogusCharsetContent()
        };

    private static StringContent BogusCharsetContent()
    {
        var content = new StringContent(Sentinel, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
        {
            CharSet = "bogus-charset"
        };
        return content;
    }

    [Fact]
    public async Task AttestAsync_MalformedBody_IncrementsAttestationFailedAndRecordsFailure()
    {
        var failed = new List<long>();
        using var listener = new System.Diagnostics.Metrics.MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == "attestation.attest.failed")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => failed.Add(value));
        listener.Start();
        var sut = Create(RespondRaw(""));

        await sut.AttestAsync(Record());

        failed.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task AttestAsync_TransportFailure_ReturnsProviderUnavailable()
    {
        var sut = Create(Throw(new HttpRequestException("connection refused")));

        var result = await sut.AttestAsync(Record());

        LeftCode(result).ShouldBe(AttestationErrors.ProviderUnavailableCode);
    }

    [Fact]
    public async Task AttestAsync_CallerCancels_ReturnsProviderUnavailableMentioningCancellation()
    {
        using var cts = new CancellationTokenSource();
        var handler = new StubHandler((_, _) =>
        {
            cts.Cancel();
            throw new TaskCanceledException();
        });
        var sut = Create(handler);

        var result = await sut.AttestAsync(Record(), cts.Token);

        var error = result.Match(_ => throw new InvalidOperationException(), l => l);
        error.GetCode().IfNone("").ShouldBe(AttestationErrors.ProviderUnavailableCode);
        error.Message.ShouldContain("cancelled");
    }

    [Fact]
    public async Task AttestAsync_HttpClientTimeout_ReturnsProviderUnavailableMentioningTimeout()
    {
        var sut = Create(Throw(new TaskCanceledException("timeout")));

        var result = await sut.AttestAsync(Record());

        var error = result.Match(_ => throw new InvalidOperationException(), l => l);
        error.GetCode().IfNone("").ShouldBe(AttestationErrors.ProviderUnavailableCode);
        error.Message.ShouldContain("timed out");
    }

    #endregion

    #region VerifyAsync

    [Fact]
    public async Task VerifyAsync_NoVerifyEndpoint_ReturnsInvalidWithExplanationWithoutCallingOut()
    {
        var handler = Respond(HttpStatusCode.OK, "{}");
        var sut = Create(handler, o => o.VerifyEndpointUrl = null);
        var receipt = Receipt();

        var verification = Right(await sut.VerifyAsync(receipt));

        verification.IsValid.ShouldBeFalse();
        verification.FailureReason.ShouldNotBeNull().ShouldContain("VerifyEndpointUrl");
        verification.AttestationId.ShouldBe(receipt.AttestationId);
        verification.VerifiedAtUtc.ShouldBe(_time.GetUtcNow());
        handler.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_RemoteSaysValid_ReturnsValidAndPostsReceiptFields()
    {
        var handler = Respond(HttpStatusCode.OK, "{\"is_valid\":true}");
        var sut = Create(handler, o => o.AuthHeader = "Bearer t");
        var receipt = Receipt();

        var verification = Right(await sut.VerifyAsync(receipt));

        verification.IsValid.ShouldBeTrue();
        verification.FailureReason.ShouldBeNull();
        verification.ProviderName.ShouldBe("Http");
        var (request, body) = handler.Calls.Single();
        request.RequestUri.ShouldBe(VerifyUrl);
        request.Headers.GetValues("Authorization").Single().ShouldBe("Bearer t");
        body.ShouldContain(receipt.AttestationId.ToString());
        body.ShouldContain("sig");
    }

    [Fact]
    public async Task VerifyAsync_RemoteSaysInvalidWithReason_ReturnsReason()
    {
        var sut = Create(Respond(HttpStatusCode.OK, "{\"is_valid\":false,\"failure_reason\":\"bad sig\"}"));

        var verification = Right(await sut.VerifyAsync(Receipt()));

        verification.IsValid.ShouldBeFalse();
        verification.FailureReason.ShouldBe("bad sig");
    }

    [Fact]
    public async Task VerifyAsync_RemoteSaysInvalidWithoutReason_UsesDefaultReason()
    {
        var sut = Create(Respond(HttpStatusCode.OK, "{\"is_valid\":false}"));

        var verification = Right(await sut.VerifyAsync(Receipt()));

        verification.IsValid.ShouldBeFalse();
        verification.FailureReason.ShouldBe("Remote verification failed.");
    }

    [Fact]
    public async Task VerifyAsync_ResponseMissingIsValid_TreatsAsInvalid()
    {
        var sut = Create(Respond(HttpStatusCode.OK, "{}"));

        var verification = Right(await sut.VerifyAsync(Receipt()));

        verification.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("not json", "application/json")]
    [InlineData("", "application/json")]
    [InlineData("plain text", "text/plain")]
    [InlineData("[true]", "application/json")]
    [InlineData("{\"is_valid\":\"true\"}", "application/json")]
    [InlineData("{\"is_valid\":1}", "application/json")]
    [InlineData("{\"is_valid\":null}", "application/json")]
    [InlineData("{\"is_valid\":{}}", "application/json")]
    public async Task VerifyAsync_UnknowableSuccessBody_ReturnsProviderUnavailable(string body, string mediaType)
    {
        var sut = Create(RespondRaw(body, mediaType));

        var result = await sut.VerifyAsync(Receipt());

        LeftCode(result).ShouldBe(AttestationErrors.ProviderUnavailableCode);
    }

    [Fact]
    public async Task VerifyAsync_MalformedBody_LeftMessageNeverContainsPayloadOrExceptionMessage()
    {
        var sut = Create(RespondRaw(Sentinel));

        var error = LeftError(await sut.VerifyAsync(Receipt()));

        error.Message.ShouldContain("JsonException");
        error.Message.ShouldNotContain(Sentinel);
    }

    [Theory]
    [InlineData("{\"is_valid\":false,\"failure_reason\":42}")]
    [InlineData("{\"is_valid\":false,\"failure_reason\":null}")]
    [InlineData("{\"is_valid\":false,\"failure_reason\":{\"a\":1}}")]
    public async Task VerifyAsync_InvalidWithWrongTypedFailureReason_UsesDefaultReason(string json)
    {
        var sut = Create(Respond(HttpStatusCode.OK, json));

        var verification = Right(await sut.VerifyAsync(Receipt()));

        verification.IsValid.ShouldBeFalse();
        verification.FailureReason.ShouldBe("Remote verification failed.");
    }

    [Fact]
    public async Task VerifyAsync_NonSuccessStatus_ReturnsInvalidMentioningStatus()
    {
        var sut = Create(Respond(HttpStatusCode.ServiceUnavailable, "down"));

        var verification = Right(await sut.VerifyAsync(Receipt()));

        verification.IsValid.ShouldBeFalse();
        verification.FailureReason.ShouldNotBeNull().ShouldContain("503");
    }

    [Fact]
    public async Task VerifyAsync_DeclaredContentLengthOverLimit_ReturnsResponseTooLarge()
    {
        var handler = new StubHandler((_, _) =>
        {
            var response = Json(HttpStatusCode.OK, "{\"is_valid\":true}");
            response.Content.Headers.ContentLength = 2_000_000;
            return Task.FromResult(response);
        });
        var sut = Create(handler);

        var result = await sut.VerifyAsync(Receipt());

        LeftCode(result).ShouldBe(AttestationErrors.HttpResponseTooLargeCode);
    }

    [Fact]
    public async Task VerifyAsync_TransportFailure_ReturnsProviderUnavailable()
    {
        var sut = Create(Throw(new HttpRequestException("refused")));

        var result = await sut.VerifyAsync(Receipt());

        LeftCode(result).ShouldBe(AttestationErrors.ProviderUnavailableCode);
    }

    [Fact]
    public async Task VerifyAsync_CallerCancels_ReturnsProviderUnavailableMentioningCancellation()
    {
        using var cts = new CancellationTokenSource();
        var handler = new StubHandler((_, _) =>
        {
            cts.Cancel();
            throw new TaskCanceledException();
        });
        var sut = Create(handler);

        var result = await sut.VerifyAsync(Receipt(), cts.Token);

        var error = result.Match(_ => throw new InvalidOperationException(), l => l);
        error.GetCode().IfNone("").ShouldBe(AttestationErrors.ProviderUnavailableCode);
        error.Message.ShouldContain("cancelled");
    }

    [Fact]
    public async Task VerifyAsync_HttpClientTimeout_ReturnsProviderUnavailableMentioningTimeout()
    {
        var sut = Create(Throw(new TaskCanceledException("timeout")));

        var result = await sut.VerifyAsync(Receipt());

        var error = result.Match(_ => throw new InvalidOperationException(), l => l);
        error.Message.ShouldContain("timed out");
    }

    #endregion

    #region Receipt retrieval

    [Fact]
    public async Task GetReceiptAsync_AlwaysReturnsReceiptNotFoundBecauseNothingIsStoredLocally()
    {
        var sut = Create(Respond(HttpStatusCode.OK, "{}"));

        var result = await sut.GetReceiptAsync(Guid.NewGuid());

        LeftCode(result).ShouldBe(AttestationErrors.ReceiptNotFoundCode);
    }

    [Fact]
    public async Task GetReceiptsAsync_ReturnsEmptyList()
    {
        var sut = Create(Respond(HttpStatusCode.OK, "{}"));

        var result = Right(await sut.GetReceiptsAsync([Guid.NewGuid(), Guid.NewGuid()]));

        result.ShouldBeEmpty();
    }

    #endregion
}
