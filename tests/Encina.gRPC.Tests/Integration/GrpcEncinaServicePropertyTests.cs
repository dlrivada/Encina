using System.Text.Json;
using Encina.gRPC;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable CA2012 // Use ValueTasks correctly - property tests require synchronous execution

namespace Encina.gRPC.Tests.Integration;

/// <summary>
/// Property-based tests for <see cref="GrpcEncinaService"/> invariants.
/// </summary>
[Trait("Category", "Property")]
[Trait("Service", "gRPC")]
public sealed class GrpcEncinaServicePropertyTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IGrpcEncinaService _grpcService;

    public GrpcEncinaServicePropertyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddEncina(typeof(GrpcEncinaServicePropertyTests).Assembly);
        services.AddEncinaGrpc();

        _serviceProvider = services.BuildServiceProvider();
        _grpcService = _serviceProvider.GetRequiredService<IGrpcEncinaService>();
    }

    [Property(MaxTest = 20)]
    public bool SendAsync_WithInvalidType_AlwaysReturnsError(PositiveInt seed)
    {
        var typeName = $"Invalid.Type{seed.Get}.DoesNotExist, FakeAssembly";
        var data = JsonSerializer.SerializeToUtf8Bytes(new { Value = "test" });

        var result = _grpcService.SendAsync(typeName, data).GetAwaiter().GetResult();

        return result.IsLeft;
    }

    [Property(MaxTest = 20)]
    public bool PublishAsync_WithInvalidType_AlwaysReturnsError(PositiveInt seed)
    {
        var typeName = $"Invalid.Type{seed.Get}.DoesNotExist, FakeAssembly";
        var data = JsonSerializer.SerializeToUtf8Bytes(new { Data = "test" });

        var result = _grpcService.PublishAsync(typeName, data).GetAwaiter().GetResult();

        return result.IsLeft;
    }

    // Skip: GrpcEncinaService has a reflection bug - see issue #520
    // [Property(MaxTest = 10)]
    // public bool SendAsync_WithValidRequest_AlwaysReturnsRight(PositiveInt seed)

    // Skip: GrpcEncinaService has a reflection bug - see issue #520
    // [Property(MaxTest = 10)]
    // public bool SendAsync_PreservesInputValue(PositiveInt seed)

    [Property(MaxTest = 10)]
    public bool StreamAsync_AlwaysReturnsNotImplemented(PositiveInt seed)
    {
        var requestType = typeof(PropertyTestQuery).AssemblyQualifiedName!;
        var requestData = JsonSerializer.SerializeToUtf8Bytes(new PropertyTestQuery($"stream-{seed.Get}"));

        var results = new List<Either<EncinaError, byte[]>>();
        var enumerator = _grpcService.StreamAsync(requestType, requestData).GetAsyncEnumerator();

        while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
        {
            results.Add(enumerator.Current);
        }

        return results.Count == 1 && results[0].IsLeft;
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}

// Test types for property tests
public sealed record PropertyTestQuery(string Value) : IQuery<PropertyTestResponse>;

public sealed record PropertyTestResponse(string Value);

public sealed class PropertyTestQueryHandler : IQueryHandler<PropertyTestQuery, PropertyTestResponse>
{
    public Task<Either<EncinaError, PropertyTestResponse>> Handle(
        PropertyTestQuery request,
        CancellationToken cancellationToken)
    {
        var response = new PropertyTestResponse($"Processed: {request.Value}");
        return Task.FromResult<Either<EncinaError, PropertyTestResponse>>(response);
    }
}
