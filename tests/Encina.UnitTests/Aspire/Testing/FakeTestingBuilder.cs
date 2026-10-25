using System.Reflection;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Aspire.Testing;

/// <summary>
/// Hand-written <see cref="IDistributedApplicationTestingBuilder"/> double whose only working member is
/// <see cref="Services"/>, a real <see cref="ServiceCollection"/>. Every other member throws
/// <see cref="NotSupportedException"/>, because <c>WithEncinaTestSupport</c> only touches the service collection.
/// </summary>
internal sealed class FakeTestingBuilder : IDistributedApplicationTestingBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();

    public ConfigurationManager Configuration => throw new NotSupportedException();

    public string AppHostDirectory => throw new NotSupportedException();

    public Assembly? AppHostAssembly => throw new NotSupportedException();

    public IHostEnvironment Environment => throw new NotSupportedException();

    public IDistributedApplicationEventing Eventing => throw new NotSupportedException();

    public DistributedApplicationExecutionContext ExecutionContext => throw new NotSupportedException();

    public IResourceCollection Resources => throw new NotSupportedException();

#pragma warning disable ASPIREPIPELINES001 // The interface member is typed with an experimental Aspire API; the double must declare it to implement the interface.
    public IDistributedApplicationPipeline Pipeline => throw new NotSupportedException();
#pragma warning restore ASPIREPIPELINES001

    public IResourceBuilder<T> AddResource<T>(T resource)
        where T : IResource => throw new NotSupportedException();

    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource)
        where T : IResource => throw new NotSupportedException();

    public DistributedApplication Build() => throw new NotSupportedException();

    public Task<DistributedApplication> BuildAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
