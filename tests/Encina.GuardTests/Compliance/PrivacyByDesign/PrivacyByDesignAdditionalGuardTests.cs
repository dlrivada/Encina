using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Health;
using Encina.Compliance.PrivacyByDesign.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.PrivacyByDesign;

/// <summary>
/// Guard tests for Encina.Compliance.PrivacyByDesign service classes whose guard
/// coverage was previously absent: InMemoryPurposeRegistry, PurposeBuilder,
/// PrivacyByDesignOptionsValidator, and ServiceCollectionExtensions.
/// </summary>
[Trait("Category", "Guard")]
public sealed class PrivacyByDesignAdditionalGuardTests
{
    // ─── InMemoryPurposeRegistry ───

    [Fact]
    public void InMemoryPurposeRegistry_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new InMemoryPurposeRegistry(null!));
    }

    [Fact]
    public void InMemoryPurposeRegistry_ValidArgs_Constructs()
    {
        var sut = new InMemoryPurposeRegistry(NullLogger<InMemoryPurposeRegistry>.Instance);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task InMemoryPurposeRegistry_GetPurposeAsync_NullName_Throws()
    {
        var sut = new InMemoryPurposeRegistry(NullLogger<InMemoryPurposeRegistry>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.GetPurposeAsync(null!));
    }

    [Fact]
    public async Task InMemoryPurposeRegistry_RegisterPurposeAsync_NullPurpose_Throws()
    {
        var sut = new InMemoryPurposeRegistry(NullLogger<InMemoryPurposeRegistry>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.RegisterPurposeAsync(null!));
    }

    [Fact]
    public async Task InMemoryPurposeRegistry_RegisterThenGet_RoundTrips()
    {
        var sut = new InMemoryPurposeRegistry(NullLogger<InMemoryPurposeRegistry>.Instance);
        var purpose = new PurposeDefinition
        {
            PurposeId = "p-1",
            Name = "OrderFulfillment",
            Description = "Process orders",
            LegalBasis = "Contract",
            AllowedFields = new[] { "ProductId" },
            ModuleId = null,
            ExpiresAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var registerResult = await sut.RegisterPurposeAsync(purpose);
        registerResult.IsRight.ShouldBeTrue();

        var getResult = await sut.GetPurposeAsync("OrderFulfillment");
        getResult.IsRight.ShouldBeTrue();
    }

    // ─── PurposeBuilder ───

    [Fact]
    public void PurposeBuilder_NullName_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PurposeBuilder(null!));
    }

    [Fact]
    public void PurposeBuilder_ValidName_Constructs()
    {
        var sut = new PurposeBuilder("Marketing");
        sut.Name.ShouldBe("Marketing");
        sut.Description.ShouldBe(string.Empty);
        sut.LegalBasis.ShouldBe(string.Empty);
        sut.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void PurposeBuilder_PropertiesSettable()
    {
        var sut = new PurposeBuilder("Marketing")
        {
            ModuleId = "core",
            Description = "Send newsletters",
            LegalBasis = "Consent"
        };

        sut.ModuleId.ShouldBe("core");
        sut.Description.ShouldBe("Send newsletters");
        sut.LegalBasis.ShouldBe("Consent");
    }

    // ─── PrivacyByDesignOptionsValidator (internal) ───

    [Fact]
    public void OptionsValidator_NullOptions_Throws()
    {
        var sut = new PrivacyByDesignOptionsValidator();
        Should.Throw<ArgumentNullException>(() => sut.Validate(null, null!));
    }

    [Fact]
    public void OptionsValidator_DefaultOptions_Succeeds()
    {
        var sut = new PrivacyByDesignOptionsValidator();
        var result = sut.Validate(null, new PrivacyByDesignOptions());
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void OptionsValidator_InvalidEnforcementMode_Fails()
    {
        var sut = new PrivacyByDesignOptionsValidator();
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = (PrivacyByDesignEnforcementMode)999
        };
        var result = sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
        result.Failures!.Any(f => f.Contains("EnforcementMode", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void OptionsValidator_InvalidPrivacyLevel_Fails()
    {
        var sut = new PrivacyByDesignOptionsValidator();
        var options = new PrivacyByDesignOptions
        {
            PrivacyLevel = (PrivacyLevel)999
        };
        var result = sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
        result.Failures!.Any(f => f.Contains("PrivacyLevel", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void OptionsValidator_OutOfRangeThreshold_Fails()
    {
        var sut = new PrivacyByDesignOptionsValidator();
        var options = new PrivacyByDesignOptions { MinimizationScoreThreshold = 1.5 };
        var result = sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
        result.Failures!.Any(f => f.Contains("MinimizationScoreThreshold", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void OptionsValidator_NegativeThreshold_Fails()
    {
        var sut = new PrivacyByDesignOptionsValidator();
        var options = new PrivacyByDesignOptions { MinimizationScoreThreshold = -0.1 };
        var result = sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void OptionsValidator_PurposeBuilderMissingDescription_Fails()
    {
        var sut = new PrivacyByDesignOptionsValidator();
        var options = new PrivacyByDesignOptions();
        options.PurposeBuilders.Add(new PurposeBuilder("test")
        {
            Description = "",
            LegalBasis = "Contract"
        });

        var result = sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
        result.Failures!.Any(f => f.Contains("Description", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void OptionsValidator_PurposeBuilderMissingLegalBasis_Fails()
    {
        var sut = new PrivacyByDesignOptionsValidator();
        var options = new PrivacyByDesignOptions();
        options.PurposeBuilders.Add(new PurposeBuilder("test")
        {
            Description = "Process orders",
            LegalBasis = ""
        });

        var result = sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
        result.Failures!.Any(f => f.Contains("LegalBasis", StringComparison.Ordinal)).ShouldBeTrue();
    }

    // ─── PrivacyByDesignHealthCheck ───

    [Fact]
    public void PrivacyByDesignHealthCheck_ConstructsWithServices()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new PrivacyByDesignHealthCheck(
            sp, NullLogger<PrivacyByDesignHealthCheck>.Instance);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaPrivacyByDesign_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaPrivacyByDesign());
    }

    [Fact]
    public void AddEncinaPrivacyByDesign_ValidServices_Registers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);

        var result = services.AddEncinaPrivacyByDesign();

        result.ShouldNotBeNull();
    }

    // ─── DataMinimizationPipelineBehavior ───

    [Fact]
    public void DataMinimizationPipelineBehavior_NullValidator_Throws()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new PrivacyByDesignOptions());
        var sp = new ServiceCollection().BuildServiceProvider();
        Should.Throw<ArgumentNullException>(() =>
            new DataMinimizationPipelineBehavior<TestRequest, TestResponse>(
                null!, options, TimeProvider.System,
                NullLogger<DataMinimizationPipelineBehavior<TestRequest, TestResponse>>.Instance,
                sp));
    }

    [Fact]
    public void DataMinimizationPipelineBehavior_NullOptions_Throws()
    {
        var validator = Substitute.For<IPrivacyByDesignValidator>();
        var sp = new ServiceCollection().BuildServiceProvider();
        Should.Throw<ArgumentNullException>(() =>
            new DataMinimizationPipelineBehavior<TestRequest, TestResponse>(
                validator, null!, TimeProvider.System,
                NullLogger<DataMinimizationPipelineBehavior<TestRequest, TestResponse>>.Instance,
                sp));
    }

    [Fact]
    public void DataMinimizationPipelineBehavior_NullTimeProvider_Throws()
    {
        var validator = Substitute.For<IPrivacyByDesignValidator>();
        var options = Microsoft.Extensions.Options.Options.Create(new PrivacyByDesignOptions());
        var sp = new ServiceCollection().BuildServiceProvider();
        Should.Throw<ArgumentNullException>(() =>
            new DataMinimizationPipelineBehavior<TestRequest, TestResponse>(
                validator, options, null!,
                NullLogger<DataMinimizationPipelineBehavior<TestRequest, TestResponse>>.Instance,
                sp));
    }

    [Fact]
    public void DataMinimizationPipelineBehavior_NullLogger_Throws()
    {
        var validator = Substitute.For<IPrivacyByDesignValidator>();
        var options = Microsoft.Extensions.Options.Options.Create(new PrivacyByDesignOptions());
        var sp = new ServiceCollection().BuildServiceProvider();
        Should.Throw<ArgumentNullException>(() =>
            new DataMinimizationPipelineBehavior<TestRequest, TestResponse>(
                validator, options, TimeProvider.System, null!, sp));
    }

    [Fact]
    public void DataMinimizationPipelineBehavior_NullServiceProvider_Throws()
    {
        var validator = Substitute.For<IPrivacyByDesignValidator>();
        var options = Microsoft.Extensions.Options.Options.Create(new PrivacyByDesignOptions());
        Should.Throw<ArgumentNullException>(() =>
            new DataMinimizationPipelineBehavior<TestRequest, TestResponse>(
                validator, options, TimeProvider.System,
                NullLogger<DataMinimizationPipelineBehavior<TestRequest, TestResponse>>.Instance,
                null!));
    }

    [Fact]
    public void DataMinimizationPipelineBehavior_ValidArgs_Constructs()
    {
        var validator = Substitute.For<IPrivacyByDesignValidator>();
        var options = Microsoft.Extensions.Options.Options.Create(new PrivacyByDesignOptions());
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new DataMinimizationPipelineBehavior<TestRequest, TestResponse>(
            validator, options, TimeProvider.System,
            NullLogger<DataMinimizationPipelineBehavior<TestRequest, TestResponse>>.Instance,
            sp);
        sut.ShouldNotBeNull();
    }

    public sealed record TestRequest : IRequest<TestResponse>;
    public sealed record TestResponse;
}
