using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Encina.Aspire.Testing;
using Encina.Messaging.Sagas;
using Encina.Testing.Fakes;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Aspire.Testing;

/// <summary>
/// Builds a real <see cref="DistributedApplication"/> over an in-process host that is never
/// started, with the same service registrations that <c>WithEncinaTestSupport</c> performs.
/// </summary>
internal sealed class AspireTestApp : IDisposable
{
    private AspireTestApp(DistributedApplication app)
    {
        App = app;
    }

    public DistributedApplication App { get; }

    public IServiceProvider Services => App.Services;

    public FakeOutboxStore Outbox => Services.GetRequiredService<FakeOutboxStore>();

    public FakeInboxStore Inbox => Services.GetRequiredService<FakeInboxStore>();

    public FakeSagaStore Sagas => Services.GetRequiredService<FakeSagaStore>();

    public FakeDeadLetterStore DeadLetters => Services.GetRequiredService<FakeDeadLetterStore>();

    /// <summary>
    /// Creates an app with Encina test support registered; the options use a short timeout
    /// and polling interval so that negative (timeout) paths finish quickly.
    /// </summary>
    public static AspireTestApp Create(bool registerTestSupport = true)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(AspireTestApp).Assembly.GetName().Name,
            ProjectDirectory = AppContext.BaseDirectory,
            DisableDashboard = true
        });

        if (registerTestSupport)
        {
            var options = new EncinaTestSupportOptions
            {
                DefaultWaitTimeout = TimeSpan.FromMilliseconds(150),
                PollingInterval = TimeSpan.FromMilliseconds(10)
            };
            builder.Services.AddSingleton(options);
            builder.Services.AddEncinaFakes();
            builder.Services.AddSingleton<EncinaTestContext>();
        }
        else
        {
            builder.Services.AddEncinaFakes();
        }

        var app = builder.Build();
        return new AspireTestApp(app);
    }

    public FakeSagaState AddSaga<TSaga>(string status = SagaStatus.Running)
    {
        var saga = new FakeSagaState
        {
            SagaType = typeof(TSaga).FullName!,
            Status = status
        };
        Sagas.AddAsync(saga).GetAwaiter().GetResult();
        return saga;
    }

    public void Dispose() => App.Dispose();
}
