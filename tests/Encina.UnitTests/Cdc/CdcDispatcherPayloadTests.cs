using System.Text.Json;

using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using Encina.UnitTests.Support;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for how <see cref="CdcDispatcher"/> turns event payloads into entities (already typed, JSON,
/// arbitrary objects, undeserializable) and for the cases where no handler can be resolved (#1557).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CdcDispatcherPayloadTests
{
    private const string Sentinel = "SENTINEL-PAYLOAD-VALUE-b17";
    private static readonly DateTime CapturedAtUtc = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    private sealed class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class RecordingHandler : IChangeEventHandler<Customer>
    {
        public List<Customer> Inserted { get; } = [];

        public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(Customer entity, ChangeContext context)
        {
            Inserted.Add(entity);
            return new(Right<EncinaError, Unit>(unit));
        }

        public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(Customer before, Customer after, ChangeContext context) =>
            new(Right<EncinaError, Unit>(unit));

        public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(Customer entity, ChangeContext context) =>
            new(Right<EncinaError, Unit>(unit));
    }

    private readonly FakeLogger<CdcDispatcher> _logger = new();
    private readonly RecordingHandler _handler = new();

    private CdcDispatcher CreateDispatcher(bool registerHandlerType = true, bool registerHandlerInstance = true)
    {
        var config = new CdcConfiguration();
        config.WithTableMapping<Customer>("customers");
        if (registerHandlerType)
        {
            config.AddHandler<Customer, RecordingHandler>();
        }

        var services = new ServiceCollection();
        if (registerHandlerInstance)
        {
            services.AddSingleton<IChangeEventHandler<Customer>>(_handler);
        }

        return new CdcDispatcher(services.BuildServiceProvider(), _logger, config);
    }

    private static ChangeEvent Event(ChangeOperation operation, object? after) =>
        new("customers", operation, null, after, new ChangeMetadata(new TestCdcPosition(1), CapturedAtUtc, null, null, null));

    [Fact]
    public async Task DispatchAsync_PayloadIsJsonElement_DeserializesIntoEntity()
    {
        using var document = JsonDocument.Parse("{\"id\":7,\"name\":\"Ada\"}");

        var result = await CreateDispatcher().DispatchAsync(Event(ChangeOperation.Insert, document.RootElement.Clone()));

        result.IsRight.ShouldBeTrue();
        _handler.Inserted.Single().Name.ShouldBe("Ada");
        _handler.Inserted.Single().Id.ShouldBe(7);
    }

    [Fact]
    public async Task DispatchAsync_PayloadIsAnonymousObject_RoundTripsThroughJson()
    {
        var result = await CreateDispatcher().DispatchAsync(Event(ChangeOperation.Insert, new { Id = 3, Name = "Grace" }));

        result.IsRight.ShouldBeTrue();
        _handler.Inserted.Single().Name.ShouldBe("Grace");
    }

    [Fact]
    public async Task DispatchAsync_PayloadAlreadyEntity_PassesSameInstance()
    {
        var entity = new Customer { Id = 1, Name = "Linus" };

        var result = await CreateDispatcher().DispatchAsync(Event(ChangeOperation.Insert, entity));

        result.IsRight.ShouldBeTrue();
        _handler.Inserted.Single().ShouldBeSameAs(entity);
    }

    [Fact]
    public async Task DispatchAsync_PayloadCannotBeDeserialized_ReturnsErrorAndLogsRedactedException()
    {
        using var document = JsonDocument.Parse($"\"{Sentinel}\"");

        var result = await CreateDispatcher().DispatchAsync(Event(ChangeOperation.Insert, document.RootElement.Clone()));

        result.IsLeft.ShouldBeTrue();
        _handler.Inserted.ShouldBeEmpty();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
    }

    [Fact]
    public async Task DispatchAsync_TableMappedWithoutHandlerRegistration_SucceedsWithoutDispatching()
    {
        var result = await CreateDispatcher(registerHandlerType: false)
            .DispatchAsync(Event(ChangeOperation.Insert, new Customer()));

        result.IsRight.ShouldBeTrue();
        _handler.Inserted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_HandlerNotResolvableFromContainer_SucceedsWithoutDispatching()
    {
        var result = await CreateDispatcher(registerHandlerInstance: false)
            .DispatchAsync(Event(ChangeOperation.Insert, new Customer()));

        result.IsRight.ShouldBeTrue();
        _handler.Inserted.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_UnknownOperation_ReturnsHandlerFailedError()
    {
        var result = await CreateDispatcher().DispatchAsync(Event((ChangeOperation)99, new Customer()));

        result.IsLeft.ShouldBeTrue();
        _handler.Inserted.ShouldBeEmpty();
    }
}
