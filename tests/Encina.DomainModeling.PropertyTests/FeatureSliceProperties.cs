using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for feature slice patterns.
/// </summary>
public class FeatureSliceProperties
{
    // === FeatureSliceConfiguration Properties ===

    [Property(MaxTest = 100)]
    public bool FeatureSliceConfiguration_AddSlice_IncreasesSliceCount()
    {
        var config = new FeatureSliceConfiguration();
        var initialCount = config.Slices.Count;

        config.AddSlice<TestFeatureSlice>();

        return config.Slices.Count == initialCount + 1;
    }

    [Property(MaxTest = 100)]
    public bool FeatureSliceConfiguration_AddSlice_AddsSliceType()
    {
        var config = new FeatureSliceConfiguration();

        config.AddSlice<TestFeatureSlice>();

        return config.SliceTypes.Contains(typeof(TestFeatureSlice));
    }

    [Property(MaxTest = 100)]
    public bool FeatureSliceConfiguration_AddSlice_FluentApi_ReturnsSameInstance()
    {
        var config = new FeatureSliceConfiguration();
        var result = config.AddSlice<TestFeatureSlice>();

        return ReferenceEquals(config, result);
    }

    [Property(MaxTest = 100)]
    public bool FeatureSliceConfiguration_ValidateDependencies_DefaultsToTrue()
    {
        var config = new FeatureSliceConfiguration();
        return config.ValidateDependencies == true;
    }

    // === FeatureSliceExtensions Properties ===

    [Property(MaxTest = 100)]
    public bool AddFeatureSlice_RegistersSliceAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddFeatureSlice<TestFeatureSlice>();

        var provider = services.BuildServiceProvider();
        var slice = provider.GetService<TestFeatureSlice>();

        return slice is not null;
    }

    [Fact]
    public void AddFeatureSlices_RegistersMultipleSlices()
    {
        var services = new ServiceCollection();
        services.AddFeatureSlices(config =>
        {
            config.AddSlice<TestFeatureSlice>();
            config.AddSlice<AnotherTestSlice>();
        });

        var provider = services.BuildServiceProvider();
        var slices = provider.GetFeatureSlices().ToList();

        // Each slice is registered for base type FeatureSlice
        Assert.Contains(slices, s => s.FeatureName == "TestFeature");
        Assert.Contains(slices, s => s.FeatureName == "AnotherFeature");
    }

    [Fact]
    public void GetFeatureSlices_ReturnsAllRegisteredSlices()
    {
        var services = new ServiceCollection();
        services.AddFeatureSlice<TestFeatureSlice>();
        services.AddFeatureSlice<AnotherTestSlice>();

        var provider = services.BuildServiceProvider();
        var slices = provider.GetFeatureSlices().ToList();

        // Each slice is registered for base type FeatureSlice
        Assert.Contains(slices, s => s.FeatureName == "TestFeature");
        Assert.Contains(slices, s => s.FeatureName == "AnotherFeature");
    }

    [Fact]
    public void GetFeatureSlice_ReturnsSliceByName()
    {
        var services = new ServiceCollection();
        services.AddFeatureSlice<TestFeatureSlice>();

        var provider = services.BuildServiceProvider();
        var slice = provider.GetFeatureSlice("TestFeature");

        Assert.NotNull(slice);
        Assert.Equal("TestFeature", slice.FeatureName);
    }

    [Property(MaxTest = 100)]
    public bool GetFeatureSlice_ReturnsNullForUnknownName()
    {
        var services = new ServiceCollection();
        services.AddFeatureSlice<TestFeatureSlice>();

        var provider = services.BuildServiceProvider();
        var slice = provider.GetFeatureSlice("UnknownFeature");

        return slice is null;
    }

    // === SliceDependency Properties ===

    [Property(MaxTest = 100)]
    public bool SliceDependency_StoresSliceName(NonEmptyString sliceName)
    {
        var dep = new SliceDependency(sliceName.Get);
        return dep.SliceName == sliceName.Get;
    }

    [Property(MaxTest = 100)]
    public bool SliceDependency_IsOptional_DefaultsToFalse(NonEmptyString sliceName)
    {
        var dep = new SliceDependency(sliceName.Get);
        return dep.IsOptional == false;
    }

    [Property(MaxTest = 100)]
    public bool SliceDependency_IsOptional_CanBeSet(NonEmptyString sliceName)
    {
        var dep = new SliceDependency(sliceName.Get, IsOptional: true);
        return dep.IsOptional == true;
    }

    // === FeatureSliceError Properties ===

    [Property(MaxTest = 100)]
    public bool FeatureSliceError_MissingDependency_HasCorrectCode(
        NonEmptyString sliceName,
        NonEmptyString depName)
    {
        var error = FeatureSliceError.MissingDependency(sliceName.Get, depName.Get);
        return error.ErrorCode == "SLICE_MISSING_DEPENDENCY"
            && error.SliceName == sliceName.Get
            && error.Message.Contains(sliceName.Get)
            && error.Message.Contains(depName.Get);
    }

    [Property(MaxTest = 100)]
    public bool FeatureSliceError_CircularDependency_HasCorrectCode()
    {
        var cycle = new List<string> { "A", "B", "C", "A" };
        var error = FeatureSliceError.CircularDependency(cycle);
        return error.ErrorCode == "SLICE_CIRCULAR_DEPENDENCY"
            && error.Message.Contains('A')
            && error.Message.Contains('B')
            && error.Message.Contains('C');
    }

    [Property(MaxTest = 100)]
    public bool FeatureSliceError_RegistrationFailed_HasCorrectCode(NonEmptyString sliceName)
    {
        var exception = new InvalidOperationException("Test error");
        var error = FeatureSliceError.RegistrationFailed(sliceName.Get, exception);
        return error.ErrorCode == "SLICE_REGISTRATION_FAILED"
            && error.SliceName == sliceName.Get
            && error.Message.Contains("Test error");
    }

    // === UseCaseHandler Properties ===

    [Property(MaxTest = 100)]
    public bool AddUseCaseHandler_RegistersHandler()
    {
        var services = new ServiceCollection();
        services.AddUseCaseHandler<TestUseCaseHandler>();

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IUseCaseHandler<TestInput, TestOutput>>();

        return handler is not null;
    }

    // Test helpers
    private sealed class TestFeatureSlice : FeatureSlice
    {
        public override string FeatureName => "TestFeature";
        public override void ConfigureServices(IServiceCollection services) { }
    }

    private sealed class AnotherTestSlice : FeatureSlice
    {
        public override string FeatureName => "AnotherFeature";
        public override void ConfigureServices(IServiceCollection services) { }
    }

    private sealed record TestInput(string Value);
    private sealed record TestOutput(string Result);

    private sealed class TestUseCaseHandler : IUseCaseHandler<TestInput, TestOutput>
    {
        public Task<TestOutput> HandleAsync(TestInput input, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestOutput(input.Value.ToUpperInvariant()));
    }
}
