using Encina.Testing.Pact;

namespace Encina.GuardTests.Testing.Pact;

public class PactExtensionsGuardTests
{
    [Fact]
    public void CreatePactHttpClient_NullUri_Throws()
    {
        Uri uri = null!;
        Should.Throw<ArgumentNullException>(() => uri.CreatePactHttpClient());
    }

    [Fact]
    public async Task SendCommandAsync_NullClient_Throws()
    {
        HttpClient client = null!;
        var command = new TestPactCommand();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            client.SendCommandAsync<TestPactCommand, string>(command));
    }

    [Fact]
    public async Task SendCommandAsync_NullCommand_Throws()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://localhost") };

        await Should.ThrowAsync<ArgumentNullException>(() =>
            client.SendCommandAsync<TestPactCommand, string>(null!));
    }

    [Fact]
    public async Task SendQueryAsync_NullClient_Throws()
    {
        HttpClient client = null!;
        var query = new TestPactQuery();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            client.SendQueryAsync<TestPactQuery, string>(query));
    }

    [Fact]
    public async Task SendQueryAsync_NullQuery_Throws()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://localhost") };

        await Should.ThrowAsync<ArgumentNullException>(() =>
            client.SendQueryAsync<TestPactQuery, string>(null!));
    }

    [Fact]
    public async Task PublishNotificationAsync_NullClient_Throws()
    {
        HttpClient client = null!;
        var notification = new TestPactNotification();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            client.PublishNotificationAsync(notification));
    }

    [Fact]
    public async Task PublishNotificationAsync_NullNotification_Throws()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://localhost") };

        await Should.ThrowAsync<ArgumentNullException>(() =>
            client.PublishNotificationAsync<TestPactNotification>(null!));
    }

    [Fact]
    public async Task ReadAsEitherAsync_NullResponse_Throws()
    {
        HttpResponseMessage response = null!;

        await Should.ThrowAsync<ArgumentNullException>(() =>
            response.ReadAsEitherAsync<string>());
    }

    // Test types for Pact extension methods
    private sealed record TestPactCommand : ICommand<string>;
    private sealed record TestPactQuery : IQuery<string>;
    private sealed record TestPactNotification : INotification;
}
