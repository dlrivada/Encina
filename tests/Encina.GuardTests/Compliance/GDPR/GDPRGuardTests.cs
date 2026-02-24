using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.GuardTests.Compliance.GDPR;

/// <summary>
/// Guard clause tests for Encina.Compliance.GDPR types.
/// Verifies that null arguments are properly rejected.
/// </summary>
public class GDPRGuardTests
{
    // -- InMemoryProcessingActivityRegistry --

    [Fact]
    public async Task Registry_RegisterActivityAsync_NullActivity_ThrowsArgumentNullException()
    {
        var sut = new InMemoryProcessingActivityRegistry();
        var act = () => sut.RegisterActivityAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("activity");
    }

    [Fact]
    public async Task Registry_GetActivityByRequestTypeAsync_NullType_ThrowsArgumentNullException()
    {
        var sut = new InMemoryProcessingActivityRegistry();
        var act = () => sut.GetActivityByRequestTypeAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("requestType");
    }

    [Fact]
    public async Task Registry_UpdateActivityAsync_NullActivity_ThrowsArgumentNullException()
    {
        var sut = new InMemoryProcessingActivityRegistry();
        var act = () => sut.UpdateActivityAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("activity");
    }

    [Fact]
    public void Registry_AutoRegisterFromAssemblies_NullAssemblies_ThrowsArgumentNullException()
    {
        var sut = new InMemoryProcessingActivityRegistry();
        Action act = () => sut.AutoRegisterFromAssemblies(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("assemblies");
    }

    // -- GDPRCompliancePipelineBehavior --

    [Fact]
    public async Task PipelineBehavior_Handle_NullRequest_ThrowsArgumentNullException()
    {
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var validator = Substitute.For<IGDPRComplianceValidator>();
        var options = Options.Create(new GDPROptions());
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GDPRCompliancePipelineBehavior<TestCommand, LanguageExt.Unit>>();

        var sut = new GDPRCompliancePipelineBehavior<TestCommand, LanguageExt.Unit>(
            registry, validator, options, logger);

        var act = () => sut.Handle(
            null!,
            RequestContext.CreateForTest(),
            () => ValueTask.FromResult<LanguageExt.Either<EncinaError, LanguageExt.Unit>>(LanguageExt.Unit.Default),
            CancellationToken.None).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    // -- GDPROptionsValidator --

    [Fact]
    public void OptionsValidator_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new GDPROptionsValidator();
        var act = () => sut.Validate(null, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    // -- ServiceCollectionExtensions --

    [Fact]
    public void AddEncinaGDPR_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var act = () => services.AddEncinaGDPR();
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    // -- JsonRoPAExporter --

    [Fact]
    public async Task JsonExporter_ExportAsync_NullActivities_ThrowsArgumentNullException()
    {
        var sut = new JsonRoPAExporter();
        var act = () => sut.ExportAsync(null!, new RoPAExportMetadata("A", "B", DateTimeOffset.UtcNow)).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("activities");
    }

    [Fact]
    public async Task JsonExporter_ExportAsync_NullMetadata_ThrowsArgumentNullException()
    {
        var sut = new JsonRoPAExporter();
        var act = () => sut.ExportAsync([], null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("metadata");
    }

    // -- CsvRoPAExporter --

    [Fact]
    public async Task CsvExporter_ExportAsync_NullActivities_ThrowsArgumentNullException()
    {
        var sut = new CsvRoPAExporter();
        var act = () => sut.ExportAsync(null!, new RoPAExportMetadata("A", "B", DateTimeOffset.UtcNow)).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("activities");
    }

    [Fact]
    public async Task CsvExporter_ExportAsync_NullMetadata_ThrowsArgumentNullException()
    {
        var sut = new CsvRoPAExporter();
        var act = () => sut.ExportAsync([], null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("metadata");
    }

    // -- ProcessingActivityMapper --

    [Fact]
    public void Mapper_ToEntity_NullActivity_ThrowsArgumentNullException()
    {
        Action act = () => ProcessingActivityMapper.ToEntity(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("activity");
    }

    [Fact]
    public void Mapper_ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Action act = () => ProcessingActivityMapper.ToDomain(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    // Test stub
    private sealed record TestCommand : ICommand<LanguageExt.Unit>;
}
