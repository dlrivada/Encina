using System.Text.Json;
using Encina.AzureFunctions.Durable;
using Encina.Messaging.Health;
using LanguageExt;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Runs the Durable Functions files that need no Functions host (<c>ActivityResult</c>,
/// <c>DurableFunctionsOptions</c>, <c>DurableServiceCollectionExtensions</c>,
/// <c>DurableFunctionsHealthCheck</c>) in process, on a provider built with <c>ValidateOnBuild</c> and
/// <c>ValidateScopes</c> next to the real Encina pipeline.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AzureFunctions")]
public sealed class DurableFunctionsInProcessIntegrationTests
{
    private static ServiceProvider BuildProvider(Action<DurableFunctionsOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddEncinaDurableFunctions(configure);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    [Fact]
    public async Task AddEncinaDurableFunctions_WithoutConfiguration_BuildsAValidProviderWithTheDefaults()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>().Value;

        // Assert
        options.DefaultMaxRetries.ShouldBe(3);
        options.DefaultFirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(5));
        options.DefaultBackoffCoefficient.ShouldBe(2.0);
        options.DefaultMaxRetryInterval.ShouldBe(TimeSpan.FromMinutes(1));
        options.ContinueCompensationOnError.ShouldBeTrue();
        options.DefaultSagaTimeout.ShouldBeNull();
        options.ProviderHealthCheck.Name.ShouldBe("encina-durable-functions");
        options.ProviderHealthCheck.Tags.ShouldBe(["encina", "durable-functions", "ready"]);
    }

    [Fact]
    public async Task AddEncinaDurableFunctions_WithConfiguration_AppliesTheConfiguredOptions()
    {
        // Arrange
        await using var provider = BuildProvider(options =>
        {
            options.DefaultMaxRetries = 7;
            options.DefaultFirstRetryInterval = TimeSpan.FromSeconds(10);
            options.DefaultBackoffCoefficient = 1.5;
            options.DefaultMaxRetryInterval = TimeSpan.FromMinutes(5);
            options.ContinueCompensationOnError = false;
            options.DefaultSagaTimeout = TimeSpan.FromHours(1);
        });

        // Act
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>().Value;

        // Assert
        options.DefaultMaxRetries.ShouldBe(7);
        options.DefaultFirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(10));
        options.DefaultBackoffCoefficient.ShouldBe(1.5);
        options.DefaultMaxRetryInterval.ShouldBe(TimeSpan.FromMinutes(5));
        options.ContinueCompensationOnError.ShouldBeFalse();
        options.DefaultSagaTimeout.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task AddEncinaDurableFunctions_RegistersTheHealthCheckOnce_EvenWhenCalledTwice()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddEncinaDurableFunctions();
        services.AddEncinaDurableFunctions(options => options.DefaultMaxRetries = 9);

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Act
        var checks = provider.GetServices<IEncinaHealthCheck>().ToList();

        // Assert
        checks.ShouldHaveSingleItem().ShouldBeOfType<DurableFunctionsHealthCheck>();
        provider.GetRequiredService<IOptions<DurableFunctionsOptions>>().Value.DefaultMaxRetries.ShouldBe(9);
    }

    [Fact]
    public async Task CheckHealthAsync_WithTheDefaultConfiguration_ReportsHealthyWithTheOptionData()
    {
        // Arrange
        await using var provider = BuildProvider();
        var check = provider.GetServices<IEncinaHealthCheck>().OfType<DurableFunctionsHealthCheck>().Single();

        // Act
        var result = await check.CheckHealthAsync(TestContext.Current.CancellationToken);

        // Assert
        check.Name.ShouldBe("encina-durable-functions");
        check.Tags.ShouldContain("durable-functions");
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["defaultMaxRetries"].ShouldBe(3);
        result.Data["continueCompensationOnError"].ShouldBe(true);
        result.Data.ContainsKey("defaultSagaTimeout").ShouldBeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_WithASagaTimeout_IncludesItInTheData()
    {
        // Arrange
        await using var provider = BuildProvider(options => options.DefaultSagaTimeout = TimeSpan.FromMinutes(30));
        var check = provider.GetServices<IEncinaHealthCheck>().OfType<DurableFunctionsHealthCheck>().Single();

        // Act
        var result = await check.CheckHealthAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["defaultSagaTimeout"].ShouldBe(TimeSpan.FromMinutes(30).ToString());
    }

    [Fact]
    public async Task CheckHealthAsync_WithACustomNameAndTags_ReportsThem()
    {
        // Arrange
        await using var provider = BuildProvider(options =>
        {
            options.ProviderHealthCheck.Name = "custom-durable";
            options.ProviderHealthCheck.Tags = ["custom"];
        });
        var check = provider.GetServices<IEncinaHealthCheck>().OfType<DurableFunctionsHealthCheck>().Single();

        // Act
        var name = check.Name;
        var tags = check.Tags;

        // Assert
        name.ShouldBe("custom-durable");
        tags.ShouldBe(["custom"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WithEveryInvalidSetting_ReportsDegradedListingEachIssue()
    {
        // Arrange
        await using var provider = BuildProvider(options =>
        {
            options.DefaultMaxRetries = -1;
            options.DefaultFirstRetryInterval = TimeSpan.Zero;
            options.DefaultBackoffCoefficient = 0;
            options.DefaultMaxRetryInterval = TimeSpan.Zero;
            options.DefaultSagaTimeout = TimeSpan.FromSeconds(-1);
        });
        var check = provider.GetServices<IEncinaHealthCheck>().OfType<DurableFunctionsHealthCheck>().Single();

        // Act
        var result = await check.CheckHealthAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        var issues = result.Data["issues"].ShouldBeAssignableTo<List<string>>()!;
        issues.Count.ShouldBe(5);
        var description = result.Description.ShouldNotBeNull();
        description.ShouldContain("DefaultMaxRetries cannot be negative");
        description.ShouldContain("DefaultSagaTimeout must be positive if set");
    }

    [Fact]
    public void ActivityResult_FromASuccess_RoundTripsThroughJson()
    {
        // Arrange
        Either<EncinaError, int> either = 42;

        // Act
        var json = JsonSerializer.Serialize(either.ToActivityResult());
        var restored = JsonSerializer.Deserialize<ActivityResult<int>>(json)!;

        // Assert
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.ShouldBe(42);
        restored.ErrorCode.ShouldBeNull();
        restored.ToEither().IsRight.ShouldBeTrue();
        json.ShouldContain("\"isSuccess\":true");
    }

    [Fact]
    public void ActivityResult_FromAFailure_RoundTripsTheErrorCodeAndMessageThroughJson()
    {
        // Arrange
        Either<EncinaError, int> either = EncinaErrors.Create("payment.declined", "Card declined");

        // Act
        var json = JsonSerializer.Serialize(either.ToActivityResult());
        var restored = JsonSerializer.Deserialize<ActivityResult<int>>(json)!;
        var back = restored.ToEither();

        // Assert
        restored.IsSuccess.ShouldBeFalse();
        restored.ErrorCode.ShouldBe("payment.declined");
        restored.ErrorMessage.ShouldBe("Card declined");
        var error = back.Match(_ => throw new InvalidOperationException("Expected a failure"), e => e);
        error.GetCode().IfNone(string.Empty).ShouldBe("payment.declined");
        error.Message.ShouldBe("Card declined");
    }

    [Fact]
    public void ActivityResult_FailureWithCodeAndMessage_ConvertsBackToTheSameError()
    {
        // Arrange
        var result = ActivityResult<string>.Failure("order.invalid", "Order is invalid");

        // Act
        var either = result.ToEither();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBeNull();
        either.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ActivityResult_FailedWithoutCodeOrMessage_FallsBackToTheDefaultError()
    {
        // Arrange
        var result = new ActivityResult<string> { IsSuccess = false };

        // Act
        var error = result.ToEither().Match(_ => throw new InvalidOperationException("Expected a failure"), e => e);

        // Assert
        error.GetCode().IfNone(string.Empty).ShouldBe("durable.activity_failed");
        error.Message.ShouldBe("Activity failed");
    }

    [Fact]
    public void ActivityResult_OfUnit_ConvertsSuccessAndFailure()
    {
        // Arrange
        Either<EncinaError, Unit> success = Unit.Default;
        Either<EncinaError, Unit> failure = EncinaErrors.Create("unit.failed", "Unit failed");

        // Act
        var ok = success.ToActivityResult();
        var ko = failure.ToActivityResult();

        // Assert
        ok.IsSuccess.ShouldBeTrue();
        ok.Value.ShouldBe(Unit.Default);
        ko.IsSuccess.ShouldBeFalse();
        ko.ErrorCode.ShouldBe("unit.failed");
    }
}
