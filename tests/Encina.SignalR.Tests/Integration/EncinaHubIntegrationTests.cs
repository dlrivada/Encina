using System.Text.Json;
using Encina.SignalR;
using LanguageExt;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.SignalR.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="EncinaHub"/> functionality.
/// Tests hub methods and type resolution with full DI setup.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Service", "SignalR")]
public sealed class EncinaHubIntegrationTests
{
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddEncina(typeof(EncinaHubIntegrationTests).Assembly);
        services.AddSignalR();
        services.AddEncinaSignalR(options =>
        {
            options.IncludeDetailedErrors = true;
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Encina_CanBeResolved_FromServiceProvider()
    {
        // Arrange
        using var sp = CreateServiceProvider();

        // Act
        var encina = sp.GetService<IEncina>();

        // Assert
        encina.ShouldNotBeNull();
    }

    [Fact]
    public async Task Encina_SendsQuery_ReturnsResponse()
    {
        // Arrange
        using var sp = CreateServiceProvider();
        var encina = sp.GetRequiredService<IEncina>();
        var query = new HubTestQuery("test-value");

        // Act
        var result = await encina.Send(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(response =>
        {
            response.Value.ShouldBe("Processed: test-value");
        });
    }

    [Fact]
    public async Task Encina_SendsCommand_ReturnsResponse()
    {
        // Arrange
        using var sp = CreateServiceProvider();
        var encina = sp.GetRequiredService<IEncina>();
        var command = new HubTestCommand("command-data");

        // Act
        var result = await encina.Send(command);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Encina_PublishesNotification_Succeeds()
    {
        // Arrange
        using var sp = CreateServiceProvider();
        var encina = sp.GetRequiredService<IEncina>();
        var notification = new HubTestNotification("notification-data");

        // Act
        var result = await encina.Publish(notification);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void SignalROptions_IncludeDetailedErrors_IsConfigured()
    {
        // Arrange
        using var sp = CreateServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<SignalROptions>>().Value;

        // Assert
        options.IncludeDetailedErrors.ShouldBeTrue();
    }

    [Fact]
    public void SignalROptions_JsonSerializerOptions_IsNullByDefault()
    {
        // Arrange - Create new provider without custom JsonSerializerOptions
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddEncinaSignalR();

        using var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<SignalROptions>>().Value;

        // Assert - JsonSerializerOptions is null by default (uses default serialization)
        options.JsonSerializerOptions.ShouldBeNull();
    }

    [Fact]
    public void SignalROptions_JsonSerializerOptions_CanBeConfigured()
    {
        // Arrange
        var customOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddEncinaSignalR(options =>
        {
            options.JsonSerializerOptions = customOptions;
        });

        using var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<SignalROptions>>().Value;

        // Assert
        options.JsonSerializerOptions.ShouldNotBeNull();
        options.JsonSerializerOptions.ShouldBeSameAs(customOptions);
    }
}

// Test types for EncinaHub integration tests

internal sealed record HubTestQuery(string Value) : IQuery<HubTestResponse>;

internal sealed record HubTestResponse(string Value);

internal sealed class HubTestQueryHandler : IQueryHandler<HubTestQuery, HubTestResponse>
{
    public Task<Either<EncinaError, HubTestResponse>> Handle(
        HubTestQuery request,
        CancellationToken cancellationToken)
    {
        var response = new HubTestResponse($"Processed: {request.Value}");
        return Task.FromResult<Either<EncinaError, HubTestResponse>>(response);
    }
}

internal sealed record HubTestCommand(string Data) : ICommand<Unit>;

internal sealed class HubTestCommandHandler : ICommandHandler<HubTestCommand, Unit>
{
    public Task<Either<EncinaError, Unit>> Handle(
        HubTestCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}

internal sealed record HubTestNotification(string Data) : INotification;

internal sealed class HubTestNotificationHandler : INotificationHandler<HubTestNotification>
{
    public Task<Either<EncinaError, Unit>> Handle(
        HubTestNotification notification,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
