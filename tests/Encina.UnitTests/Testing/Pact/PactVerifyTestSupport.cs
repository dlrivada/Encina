using System.Text.Json;
using System.Text.Json.Nodes;

using Encina.Testing.Pact;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Testing.Pact;

// Type names are prefixed with "PactVerify" because EncinaPactProviderVerifier resolves request
// types by simple name across every loaded assembly; the prefix keeps the names unique.

/// <summary>Command used by the provider verifier tests.</summary>
public sealed record PactVerifyCreateOrderCommand(Guid OrderId, string CustomerName) : ICommand<PactVerifyOrderDto>;

/// <summary>Query whose result is chosen by <see cref="Scenario"/>.</summary>
public sealed record PactVerifyGetOrderQuery(Guid OrderId, string Scenario) : IQuery<PactVerifyOrderDto>;

/// <summary>Query that returns arrays, numbers, booleans and nulls.</summary>
public sealed record PactVerifyListOrdersQuery(string Scenario) : IQuery<PactVerifyOrderListDto>;

/// <summary>Notification; a customer named "reject" makes the handler fail.</summary>
public sealed record PactVerifyOrderPlacedNotification(Guid OrderId, string CustomerName) : INotification;

/// <summary>A type that is neither a request nor a notification.</summary>
public sealed record PactVerifyNotARequest(string Value);

/// <summary>Order DTO.</summary>
public sealed record PactVerifyOrderDto
{
    public Guid Id { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Status { get; init; } = "Created";
}

/// <summary>Order list DTO covering every JSON value kind.</summary>
public sealed record PactVerifyOrderListDto
{
    public List<string> Tags { get; init; } = [];
    public List<int> Quantities { get; init; } = [];
    public bool IsPaid { get; init; }
    public string? Note { get; init; }
    public decimal Total { get; init; }
    public List<PactVerifyLineDto> Lines { get; init; } = [];
}

/// <summary>Order line DTO.</summary>
public sealed record PactVerifyLineDto(string Sku, int Quantity);

internal sealed class PactVerifyCreateOrderHandler : IRequestHandler<PactVerifyCreateOrderCommand, PactVerifyOrderDto>
{
    public Task<Either<EncinaError, PactVerifyOrderDto>> Handle(
        PactVerifyCreateOrderCommand request,
        CancellationToken cancellationToken)
        => Task.FromResult(Either<EncinaError, PactVerifyOrderDto>.Right(
            new PactVerifyOrderDto { Id = request.OrderId, CustomerName = request.CustomerName }));
}

internal sealed class PactVerifyGetOrderHandler : IRequestHandler<PactVerifyGetOrderQuery, PactVerifyOrderDto>
{
    public Task<Either<EncinaError, PactVerifyOrderDto>> Handle(
        PactVerifyGetOrderQuery request,
        CancellationToken cancellationToken)
    {
        Either<EncinaError, PactVerifyOrderDto> result = request.Scenario switch
        {
            "ok" => new PactVerifyOrderDto { Id = request.OrderId, CustomerName = "Ada" },
            "validation" => EncinaErrors.Create("encina.validation.invalid", "The order id is invalid."),
            "forbidden" => EncinaErrors.Forbidden(),
            _ => EncinaErrors.Create("custom.failure", "Something unmapped failed.")
        };

        return Task.FromResult(result);
    }
}

internal sealed class PactVerifyListOrdersHandler : IRequestHandler<PactVerifyListOrdersQuery, PactVerifyOrderListDto>
{
    public Task<Either<EncinaError, PactVerifyOrderListDto>> Handle(
        PactVerifyListOrdersQuery request,
        CancellationToken cancellationToken)
        => Task.FromResult(Either<EncinaError, PactVerifyOrderListDto>.Right(new PactVerifyOrderListDto
        {
            Tags = ["a", "b"],
            Quantities = [1, 2],
            IsPaid = true,
            Note = null,
            Total = 12.5m,
            Lines = [new PactVerifyLineDto("sku-1", 1)]
        }));
}

internal sealed class PactVerifyOrderPlacedHandler : INotificationHandler<PactVerifyOrderPlacedNotification>
{
    public Task<Either<EncinaError, Unit>> Handle(
        PactVerifyOrderPlacedNotification notification,
        CancellationToken cancellationToken)
    {
        Either<EncinaError, Unit> result = string.Equals(notification.CustomerName, "reject", StringComparison.Ordinal)
            ? EncinaErrors.Create("encina.validation.rejected", "The customer was rejected.")
            : Unit.Default;

        return Task.FromResult(result);
    }
}

/// <summary>
/// Builds a real Encina container with the verifier test handlers and writes real Pact files into a
/// per-instance temporary folder that is deleted on dispose.
/// </summary>
internal sealed class PactVerifyHarness : IDisposable
{
    private readonly ServiceProvider _rootProvider;
    private readonly IServiceScope _scope;
    private int _fileCounter;

    public PactVerifyHarness()
    {
        PactDirectory = Path.Combine(Path.GetTempPath(), $"pact-verify-{Guid.NewGuid():N}");
        Directory.CreateDirectory(PactDirectory);

        var services = new ServiceCollection();
        Register(services);
        _rootProvider = services.BuildServiceProvider();
        _scope = _rootProvider.CreateScope();
        Verifier = new EncinaPactProviderVerifier(
            _scope.ServiceProvider.GetRequiredService<IEncina>(),
            _scope.ServiceProvider);
    }

    public string PactDirectory { get; }

    public EncinaPactProviderVerifier Verifier { get; }

    public static void Register(IServiceCollection services)
    {
        services.AddLogging();
        services.AddEncina();
        services.AddTransient<IRequestHandler<PactVerifyCreateOrderCommand, PactVerifyOrderDto>, PactVerifyCreateOrderHandler>();
        services.AddTransient<IRequestHandler<PactVerifyGetOrderQuery, PactVerifyOrderDto>, PactVerifyGetOrderHandler>();
        services.AddTransient<IRequestHandler<PactVerifyListOrdersQuery, PactVerifyOrderListDto>, PactVerifyListOrdersHandler>();
        services.AddTransient<INotificationHandler<PactVerifyOrderPlacedNotification>, PactVerifyOrderPlacedHandler>();
    }

    /// <summary>Builds one Pact interaction as a JSON object.</summary>
    public static JsonObject Interaction(
        string description,
        string? path,
        string? requestBodyJson,
        int? status,
        string? responseBodyJson = null,
        string? providerState = null,
        JsonArray? providerStates = null)
    {
        var request = new JsonObject { ["method"] = "POST" };
        if (path is not null)
        {
            request["path"] = path;
        }

        request["body"] = requestBodyJson is null ? null : JsonNode.Parse(requestBodyJson);

        var response = new JsonObject();
        if (status is not null)
        {
            response["status"] = status;
        }

        if (responseBodyJson is not null)
        {
            response["body"] = JsonNode.Parse(responseBodyJson);
        }

        var interaction = new JsonObject
        {
            ["description"] = description,
            ["request"] = request,
            ["response"] = response
        };

        if (providerState is not null)
        {
            interaction["providerState"] = providerState;
        }

        if (providerStates is not null)
        {
            interaction["providerStates"] = providerStates;
        }

        return interaction;
    }

    /// <summary>Writes a Pact file with the given interactions and returns its path.</summary>
    public async Task<string> WritePactAsync(params JsonObject[] interactions)
    {
        var interactionArray = new JsonArray();
        foreach (var interaction in interactions)
        {
            interactionArray.Add(interaction.DeepClone());
        }

        var pact = new JsonObject
        {
            ["consumer"] = new JsonObject { ["name"] = "WebApp" },
            ["provider"] = new JsonObject { ["name"] = "OrdersApi" },
            ["interactions"] = interactionArray
        };

        return await WriteRawAsync(pact.ToJsonString(new JsonSerializerOptions()));
    }

    /// <summary>Writes arbitrary text as a pact file and returns its path.</summary>
    public async Task<string> WriteRawAsync(string content)
    {
        var path = Path.Combine(PactDirectory, $"pact-{Interlocked.Increment(ref _fileCounter)}.json");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    public void Dispose()
    {
        Verifier.Dispose();
        _scope.Dispose();
        _rootProvider.Dispose();

        if (Directory.Exists(PactDirectory))
        {
            Directory.Delete(PactDirectory, recursive: true);
        }
    }
}
