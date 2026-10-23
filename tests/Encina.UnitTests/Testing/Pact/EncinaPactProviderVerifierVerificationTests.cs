using System.Text.Json.Nodes;

using Encina.Testing.Pact;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Testing.Pact;

/// <summary>
/// Runs <see cref="EncinaPactProviderVerifier.VerifyAsync"/> against real Pact files and real Encina
/// handlers; the private matching, status mapping, handler invocation and provider state code is
/// reached through that public entry point.
/// </summary>
public sealed class EncinaPactProviderVerifierVerificationTests : IDisposable
{
    private const string CreateOrderPath = "/api/commands/PactVerifyCreateOrderCommand";
    private const string GetOrderPath = "/api/queries/PactVerifyGetOrderQuery";
    private const string ListOrdersPath = "/api/queries/PactVerifyListOrdersQuery";
    private const string NotificationPath = "/api/notifications/PactVerifyOrderPlacedNotification";

    private static readonly Guid OrderId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly PactVerifyHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private static string OrderRequest(string scenario) =>
        $$"""{"orderId":"{{OrderId}}","scenario":"{{scenario}}"}""";

    private async Task<PactVerificationResult> VerifyAsync(params JsonObject[] interactions) =>
        await _harness.Verifier.VerifyAsync(await _harness.WritePactAsync(interactions));

    [Fact]
    public async Task VerifyAsync_CommandWithMatchingResponse_Succeeds()
    {
        var interaction = PactVerifyHarness.Interaction(
            "creates an order",
            CreateOrderPath,
            $$"""{"orderId":"{{OrderId}}","customerName":"Ada"}""",
            200,
            $$"""{"id":"{{OrderId}}","customerName":"Ada","status":"Created"}""");

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.InteractionResults.Count.ShouldBe(1);
        result.InteractionResults[0].Description.ShouldBe("creates an order");
        result.InteractionResults[0].ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task VerifyAsync_ResponseWithoutStatus_DefaultsToOk()
    {
        var interaction = PactVerifyHarness.Interaction(
            "query with default status",
            GetOrderPath,
            OrderRequest("ok"),
            status: null);

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_StatusMismatch_ReportsExpectedAndActual()
    {
        var interaction = PactVerifyHarness.Interaction(
            "wrong status",
            GetOrderPath,
            OrderRequest("ok"),
            201);

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldBe("Status code mismatch: expected 201, got 200");
        result.Errors.Single().ShouldContain("Interaction 'wrong status' failed");
    }

    [Fact]
    public async Task VerifyAsync_BodyValueMismatch_ReportsBodyMismatch()
    {
        var interaction = PactVerifyHarness.Interaction(
            "wrong body",
            GetOrderPath,
            OrderRequest("ok"),
            200,
            """{"customerName":"Grace"}""");

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldStartWith("Response body mismatch.");
    }

    [Theory]
    [InlineData("validation", 400, "encina.validation.invalid")]
    [InlineData("forbidden", 403, "encina.authorization.forbidden")]
    [InlineData("unmapped", 500, "custom.failure")]
    public async Task VerifyAsync_HandlerError_MapsToExpectedStatusAndCarriesErrorCode(
        string scenario,
        int expectedStatus,
        string expectedCode)
    {
        var interaction = PactVerifyHarness.Interaction(
            $"query fails with {scenario}",
            GetOrderPath,
            OrderRequest(scenario),
            expectedStatus,
            $$"""{"isSuccess":false,"errorCode":"{{expectedCode}}"}""");

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
    }

    [Fact]
    public async Task VerifyAsync_ErrorWithUnexpectedStatus_Fails()
    {
        var interaction = PactVerifyHarness.Interaction(
            "validation error expected as success",
            GetOrderPath,
            OrderRequest("validation"),
            200);

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldBe("Status code mismatch: expected 200, got 400");
    }

    [Fact]
    public async Task VerifyAsync_NotificationHandled_ReturnsReceivedResponse()
    {
        var interaction = PactVerifyHarness.Interaction(
            "order placed",
            NotificationPath,
            $$"""{"orderId":"{{OrderId}}","customerName":"Ada"}""",
            200,
            """{"received":true}""");

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
    }

    [Fact]
    public async Task VerifyAsync_NotificationHandlerFails_MapsErrorToBadRequest()
    {
        var interaction = PactVerifyHarness.Interaction(
            "order rejected",
            NotificationPath,
            $$"""{"orderId":"{{OrderId}}","customerName":"reject"}""",
            400,
            """{"isSuccess":false,"errorCode":"encina.validation.rejected"}""");

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
    }

    [Fact]
    public async Task VerifyAsync_NotificationCategoryWithNonNotificationType_ReturnsBadRequest()
    {
        var interaction = PactVerifyHarness.Interaction(
            "not a notification",
            "/api/notifications/PactVerifyNotARequest",
            """{"value":"x"}""",
            400,
            """{"errorCode":"encina.invalid"}""");

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
    }

    [Fact]
    public async Task VerifyAsync_CommandCategoryWithNonRequestType_ReturnsBadRequest()
    {
        var interaction = PactVerifyHarness.Interaction(
            "not a request",
            "/api/commands/PactVerifyNotARequest",
            """{"value":"x"}""",
            400,
            """{"errorCode":"encina.invalid"}""");

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
    }

    [Fact]
    public async Task VerifyAsync_UnknownCategory_ReturnsBadRequestWithUnknownCode()
    {
        var interaction = PactVerifyHarness.Interaction(
            "unknown category",
            "/api/widgets/PactVerifyCreateOrderCommand",
            $$"""{"orderId":"{{OrderId}}","customerName":"Ada"}""",
            400,
            """{"errorCode":"encina.unknown"}""");

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"tags":["a","b"]}""")]
    [InlineData("""{"quantities":[1,2],"isPaid":true,"total":12.5}""")]
    [InlineData("""{"note":null}""")]
    [InlineData("""{"lines":[{"sku":"sku-1"}]}""")]
    [InlineData("""{"tags":["a","b"],"quantities":[1,2],"isPaid":true,"note":null,"total":12.5,"lines":[{"sku":"sku-1","quantity":1}]}""")]
    public async Task VerifyAsync_ExpectedBodyIsSubsetOfActual_Succeeds(string expectedBody)
    {
        var interaction = PactVerifyHarness.Interaction(
            "subset match",
            ListOrdersPath,
            """{"scenario":"full"}""",
            200,
            expectedBody);

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("""{"tags":["a"]}""")]
    [InlineData("""{"tags":["a","z"]}""")]
    [InlineData("""{"quantities":[1,3]}""")]
    [InlineData("""{"quantities":["1","2"]}""")]
    [InlineData("""{"isPaid":false}""")]
    [InlineData("""{"note":"unexpected"}""")]
    [InlineData("""{"total":99}""")]
    [InlineData("""{"missing":1}""")]
    [InlineData("""{"lines":[{"sku":"other","quantity":1}]}""")]
    [InlineData("""{"lines":[]}""")]
    [InlineData("""[1]""")]
    public async Task VerifyAsync_ExpectedBodyDiffersFromActual_ReportsBodyMismatch(string expectedBody)
    {
        var interaction = PactVerifyHarness.Interaction(
            "mismatching body",
            ListOrdersPath,
            """{"scenario":"full"}""",
            200,
            expectedBody);

        var result = await VerifyAsync(interaction);

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldNotBeNull().ShouldStartWith("Response body mismatch.");
    }

    [Fact]
    public async Task VerifyAsync_Version3ProviderStateWithParameters_InvokesParameterHandler()
    {
        IDictionary<string, object>? received = null;
        _harness.Verifier.WithProviderState("order exists", (IDictionary<string, object> p) =>
        {
            received = p;
            return Task.CompletedTask;
        });
        var states = new JsonArray(new JsonObject
        {
            ["name"] = "order exists",
            ["params"] = new JsonObject { ["id"] = 5 }
        });

        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "with state",
            GetOrderPath,
            OrderRequest("ok"),
            200,
            providerStates: states));

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
        received.ShouldNotBeNull();
        received.ContainsKey("id").ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_ProviderStateWithParametersButOnlyPlainHandler_InvokesPlainHandler()
    {
        var calls = new List<string>();
        _harness.Verifier.WithProviderState("order exists", () => calls.Add("plain"));
        var states = new JsonArray(new JsonObject
        {
            ["name"] = "order exists",
            ["params"] = new JsonObject { ["id"] = 5 }
        });

        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "with state",
            GetOrderPath,
            OrderRequest("ok"),
            200,
            providerStates: states));

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
        calls.ShouldBe(["plain"]);
    }

    [Fact]
    public async Task VerifyAsync_Version2ProviderState_InvokesAsyncHandlerOnceCaseInsensitively()
    {
        var calls = 0;
        _harness.Verifier.WithProviderState("An Order Exists", () =>
        {
            calls++;
            return Task.CompletedTask;
        });

        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "v2 state",
            GetOrderPath,
            OrderRequest("ok"),
            200,
            providerState: "an order exists"));

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task VerifyAsync_MissingProviderStateInLenientMode_ReportsThroughCallbackAndContinues()
    {
        var messages = new List<string>();
        _harness.Verifier.OnMissingProviderState(messages.Add);
        var states = new JsonArray(new JsonObject
        {
            ["name"] = "unknown state",
            ["params"] = new JsonObject { ["id"] = 5 }
        });

        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "lenient",
            GetOrderPath,
            OrderRequest("ok"),
            200,
            providerStates: states));

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
        var message = messages.ShouldHaveSingleItem();
        message.ShouldContain("No provider state handler registered for 'unknown state'");
        message.ShouldContain("with parameters: id=5");
    }

    [Fact]
    public async Task VerifyAsync_ParameterHandlerWithoutParameters_ReportsMissingState()
    {
        var messages = new List<string>();
        _harness.Verifier
            .WithProviderState("needs params", (IDictionary<string, object> _) => Task.CompletedTask)
            .OnMissingProviderState(messages.Add);

        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "v2 state without params",
            GetOrderPath,
            OrderRequest("ok"),
            200,
            providerState: "needs params"));

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
        messages.ShouldHaveSingleItem().ShouldNotContain("with parameters");
    }

    [Fact]
    public async Task VerifyAsync_MissingProviderStateInStrictMode_FailsInteraction()
    {
        _harness.Verifier.WithStrictProviderStates();

        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "strict",
            GetOrderPath,
            OrderRequest("ok"),
            200,
            providerState: "absent state"));

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldNotBeNull()
            .ShouldContain("No provider state handler registered for 'absent state'");
    }

    [Fact]
    public async Task VerifyAsync_ProviderStateEntryWithoutName_IsIgnored()
    {
        var messages = new List<string>();
        _harness.Verifier.OnMissingProviderState(messages.Add);
        var states = new JsonArray(new JsonObject { ["name"] = "" });

        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "nameless state",
            GetOrderPath,
            OrderRequest("ok"),
            200,
            providerStates: states));

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_MissingRequestPath_FailsInteraction()
    {
        var result = await VerifyAsync(PactVerifyHarness.Interaction("no path", null, "{}", 200));

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldBe("Request path is missing");
    }

    [Fact]
    public async Task VerifyAsync_PathWithoutTypeSegment_FailsInteraction()
    {
        var result = await VerifyAsync(PactVerifyHarness.Interaction("short path", "/api/commands", "{}", 200));

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldBe("Could not determine request type from path");
    }

    [Fact]
    public async Task VerifyAsync_UnknownRequestType_FailsInteraction()
    {
        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "unknown type",
            "/api/commands/PactVerifyDefinitelyMissingType",
            "{}",
            200));

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldBe(
            "Request type 'PactVerifyDefinitelyMissingType' not found in loaded assemblies");
    }

    [Fact]
    public async Task VerifyAsync_KnownRequestTypeLookedUpTwice_UsesCachedType()
    {
        var interaction = PactVerifyHarness.Interaction(
            "cached lookup",
            GetOrderPath,
            OrderRequest("ok"),
            200);

        var result = await VerifyAsync(interaction, interaction);

        result.Success.ShouldBeTrue(string.Join("; ", result.Errors));
        result.InteractionResults.Count.ShouldBe(2);
    }

    [Fact]
    public async Task VerifyAsync_RequestBodyOfWrongShape_ReportsDeserializationFailure()
    {
        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "bad body",
            CreateOrderPath,
            "\"not-an-object\"",
            200));

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldNotBeNull()
            .ShouldStartWith("Failed to deserialize request body:");
    }

    [Fact]
    public async Task VerifyAsync_NullRequestBody_ReportsNullRequest()
    {
        var result = await VerifyAsync(PactVerifyHarness.Interaction(
            "null body",
            CreateOrderPath,
            requestBodyJson: null,
            200));

        result.Success.ShouldBeFalse();
        result.InteractionResults[0].ErrorMessage.ShouldBe("Request body deserialized to null");
    }

    [Fact]
    public async Task VerifyAsync_MixedInteractions_ReportsOnlyFailingOnes()
    {
        var good = PactVerifyHarness.Interaction("good", GetOrderPath, OrderRequest("ok"), 200);
        var bad = PactVerifyHarness.Interaction("bad", GetOrderPath, OrderRequest("ok"), 404);

        var result = await VerifyAsync(good, bad);

        result.Success.ShouldBeFalse();
        result.InteractionResults.Select(r => r.Success).ShouldBe([true, false]);
        result.Errors.ShouldHaveSingleItem().ShouldContain("Interaction 'bad' failed");
    }

    [Fact]
    public async Task VerifyAsync_PactWithoutInteractions_Succeeds()
    {
        var path = await _harness.WriteRawAsync("""{"consumer":{"name":"WebApp"},"provider":{"name":"OrdersApi"}}""");

        var result = await _harness.Verifier.VerifyAsync(path);

        result.Success.ShouldBeTrue();
        result.InteractionResults.ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_MissingFile_ReportsFileNotFound()
    {
        var path = Path.Combine(_harness.PactDirectory, "does-not-exist.json");

        var result = await _harness.Verifier.VerifyAsync(path);

        result.Success.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ShouldBe($"Pact file not found: {path}");
    }

    [Fact]
    public async Task VerifyAsync_InvalidJson_ReportsVerificationError()
    {
        var path = await _harness.WriteRawAsync("{ this is not json");

        var result = await _harness.Verifier.VerifyAsync(path);

        result.Success.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ShouldStartWith("Verification error:");
    }

    [Fact]
    public async Task VerifyAsync_JsonNullDocument_ReportsParseFailure()
    {
        var path = await _harness.WriteRawAsync("null");

        var result = await _harness.Verifier.VerifyAsync(path);

        result.Success.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ShouldBe("Failed to parse Pact file");
    }

    [Fact]
    public async Task VerifyAsync_AfterDispose_Throws()
    {
        var path = await _harness.WriteRawAsync("{}");
        _harness.Verifier.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(() => _harness.Verifier.VerifyAsync(path));
    }

    [Fact]
    public async Task VerifyAsync_CancelledToken_PropagatesCancellation()
    {
        var path = await _harness.WriteRawAsync("{}");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => _harness.Verifier.VerifyAsync(path, cts.Token));
    }

    [Fact]
    public async Task VerifyAsync_HandlersNotRegistered_FailsWithoutThrowing()
    {
        // A verifier whose container has no Encina registration cannot resolve handlers.
        using var provider = new ServiceCollection().AddLogging().AddEncina().BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var verifier = new EncinaPactProviderVerifier(
            scope.ServiceProvider.GetRequiredService<IEncina>(),
            scope.ServiceProvider);
        var path = await _harness.WritePactAsync(PactVerifyHarness.Interaction(
            "no handler",
            GetOrderPath,
            OrderRequest("ok"),
            200));

        var result = await verifier.VerifyAsync(path);

        result.Success.ShouldBeFalse();
        result.InteractionResults.ShouldHaveSingleItem().Success.ShouldBeFalse();
    }
}
