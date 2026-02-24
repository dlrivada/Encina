using Encina.Compliance.GDPR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.GuardTests.Compliance.GDPR;

/// <summary>
/// Guard clause tests for Encina.Compliance.GDPR lawful basis types.
/// Verifies that null arguments are properly rejected.
/// </summary>
public class LawfulBasisGuardTests
{
    // ================================================================
    // InMemoryLawfulBasisRegistry
    // ================================================================

    /// <summary>
    /// Verifies that <see cref="InMemoryLawfulBasisRegistry.RegisterAsync"/> rejects a null registration.
    /// </summary>
    [Fact]
    public async Task Registry_RegisterAsync_NullRegistration_ThrowsArgumentNullException()
    {
        var sut = new InMemoryLawfulBasisRegistry();
        var act = () => sut.RegisterAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("registration");
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryLawfulBasisRegistry.GetByRequestTypeAsync"/> rejects a null request type.
    /// </summary>
    [Fact]
    public async Task Registry_GetByRequestTypeAsync_NullType_ThrowsArgumentNullException()
    {
        var sut = new InMemoryLawfulBasisRegistry();
        var act = () => sut.GetByRequestTypeAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("requestType");
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryLawfulBasisRegistry.GetByRequestTypeNameAsync"/> rejects a null name.
    /// </summary>
    [Fact]
    public async Task Registry_GetByRequestTypeNameAsync_NullName_ThrowsArgumentNullException()
    {
        var sut = new InMemoryLawfulBasisRegistry();
        var act = () => sut.GetByRequestTypeNameAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("requestTypeName");
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryLawfulBasisRegistry.AutoRegisterFromAssemblies"/> rejects null assemblies.
    /// </summary>
    [Fact]
    public void Registry_AutoRegisterFromAssemblies_NullAssemblies_ThrowsArgumentNullException()
    {
        var sut = new InMemoryLawfulBasisRegistry();
        Action act = () => sut.AutoRegisterFromAssemblies(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("assemblies");
    }

    // ================================================================
    // InMemoryLIAStore
    // ================================================================

    /// <summary>
    /// Verifies that <see cref="InMemoryLIAStore.StoreAsync"/> rejects a null record.
    /// </summary>
    [Fact]
    public async Task LIAStore_StoreAsync_NullRecord_ThrowsArgumentNullException()
    {
        var sut = new InMemoryLIAStore();
        var act = () => sut.StoreAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("record");
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryLIAStore.GetByReferenceAsync"/> rejects a null reference.
    /// </summary>
    [Fact]
    public async Task LIAStore_GetByReferenceAsync_NullReference_ThrowsArgumentNullException()
    {
        var sut = new InMemoryLIAStore();
        var act = () => sut.GetByReferenceAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("liaReference");
    }

    // ================================================================
    // DefaultLawfulBasisProvider
    // ================================================================

    /// <summary>
    /// Verifies that <see cref="DefaultLawfulBasisProvider"/> constructor rejects a null registry.
    /// </summary>
    [Fact]
    public void DefaultProvider_NullRegistry_ThrowsArgumentNullException()
    {
        Action act = () => _ = new DefaultLawfulBasisProvider(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    /// <summary>
    /// Verifies that <see cref="DefaultLawfulBasisProvider.GetBasisForRequestAsync"/> rejects a null request type.
    /// </summary>
    [Fact]
    public async Task DefaultProvider_GetBasisForRequestAsync_NullType_ThrowsArgumentNullException()
    {
        var registry = Substitute.For<ILawfulBasisRegistry>();
        var sut = new DefaultLawfulBasisProvider(registry);
        var act = () => sut.GetBasisForRequestAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("requestType");
    }

    // ================================================================
    // DefaultLegitimateInterestAssessment
    // ================================================================

    /// <summary>
    /// Verifies that <see cref="DefaultLegitimateInterestAssessment"/> constructor rejects a null store.
    /// </summary>
    [Fact]
    public void DefaultLIA_NullStore_ThrowsArgumentNullException()
    {
        Action act = () => _ = new DefaultLegitimateInterestAssessment(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("store");
    }

    /// <summary>
    /// Verifies that <see cref="DefaultLegitimateInterestAssessment.ValidateAsync"/> rejects a null reference.
    /// </summary>
    [Fact]
    public async Task DefaultLIA_ValidateAsync_NullReference_ThrowsArgumentNullException()
    {
        var store = Substitute.For<ILIAStore>();
        var sut = new DefaultLegitimateInterestAssessment(store);
        var act = () => sut.ValidateAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("liaReference");
    }

    // ================================================================
    // LawfulBasisRegistration.FromAttribute
    // ================================================================

    /// <summary>
    /// Verifies that <see cref="LawfulBasisRegistration.FromAttribute"/> rejects a null type.
    /// </summary>
    [Fact]
    public void Registration_FromAttribute_NullType_ThrowsArgumentNullException()
    {
        Action act = () => LawfulBasisRegistration.FromAttribute(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("requestType");
    }

    // ================================================================
    // LawfulBasisAttribute
    // ================================================================

    /// <summary>
    /// Verifies that <see cref="LawfulBasisAttribute"/> stores the declared basis.
    /// </summary>
    [Fact]
    public void Attribute_StoresBasis()
    {
        var attr = new LawfulBasisAttribute(LawfulBasis.Consent);
        attr.Basis.ShouldBe(LawfulBasis.Consent);
    }

    // ================================================================
    // LawfulBasisValidationPipelineBehavior
    // ================================================================

    /// <summary>
    /// Verifies that <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> constructor
    /// rejects a null registry.
    /// </summary>
    [Fact]
    public void PipelineBehavior_NullRegistry_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>(
            null!,
            Substitute.For<ILegitimateInterestAssessment>(),
            Substitute.For<ILawfulBasisSubjectIdExtractor>(),
            Options.Create(new LawfulBasisOptions()),
            new NullLogger<LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>>());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    /// <summary>
    /// Verifies that <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> constructor
    /// rejects a null LIA assessment.
    /// </summary>
    [Fact]
    public void PipelineBehavior_NullLiaAssessment_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>(
            Substitute.For<ILawfulBasisRegistry>(),
            null!,
            Substitute.For<ILawfulBasisSubjectIdExtractor>(),
            Options.Create(new LawfulBasisOptions()),
            new NullLogger<LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>>());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("liaAssessment");
    }

    /// <summary>
    /// Verifies that <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> constructor
    /// rejects a null subject ID extractor.
    /// </summary>
    [Fact]
    public void PipelineBehavior_NullExtractor_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>(
            Substitute.For<ILawfulBasisRegistry>(),
            Substitute.For<ILegitimateInterestAssessment>(),
            null!,
            Options.Create(new LawfulBasisOptions()),
            new NullLogger<LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>>());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("subjectIdExtractor");
    }

    /// <summary>
    /// Verifies that <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> constructor
    /// rejects null options.
    /// </summary>
    [Fact]
    public void PipelineBehavior_NullOptions_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>(
            Substitute.For<ILawfulBasisRegistry>(),
            Substitute.For<ILegitimateInterestAssessment>(),
            Substitute.For<ILawfulBasisSubjectIdExtractor>(),
            null!,
            new NullLogger<LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>>());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> constructor
    /// rejects a null logger.
    /// </summary>
    [Fact]
    public void PipelineBehavior_NullLogger_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>(
            Substitute.For<ILawfulBasisRegistry>(),
            Substitute.For<ILegitimateInterestAssessment>(),
            Substitute.For<ILawfulBasisSubjectIdExtractor>(),
            Options.Create(new LawfulBasisOptions()),
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that <see cref="LawfulBasisValidationPipelineBehavior{TRequest,TResponse}.Handle"/>
    /// rejects a null request.
    /// </summary>
    [Fact]
    public async Task PipelineBehavior_Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateBehavior();
        var act = () => sut.Handle(
            null!,
            RequestContext.CreateForTest(),
            () => ValueTask.FromResult<LanguageExt.Either<EncinaError, LanguageExt.Unit>>(LanguageExt.Unit.Default),
            CancellationToken.None).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    // ================================================================
    // LawfulBasisOptions fluent API
    // ================================================================

    /// <summary>
    /// Verifies that <see cref="LawfulBasisOptions.ScanAssembly"/> rejects a null assembly.
    /// </summary>
    [Fact]
    public void Options_ScanAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        var options = new LawfulBasisOptions();
        Action act = () => options.ScanAssembly(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("assembly");
    }

    // ================================================================
    // Helpers
    // ================================================================

    private sealed record TestCommand : ICommand<LanguageExt.Unit>;

    private static LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit> CreateBehavior(
        ILawfulBasisRegistry? registry = null,
        ILegitimateInterestAssessment? liaAssessment = null,
        ILawfulBasisSubjectIdExtractor? subjectIdExtractor = null,
        IOptions<LawfulBasisOptions>? options = null,
        ILogger<LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>>? logger = null)
    {
        return new LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>(
            registry ?? Substitute.For<ILawfulBasisRegistry>(),
            liaAssessment ?? Substitute.For<ILegitimateInterestAssessment>(),
            subjectIdExtractor ?? Substitute.For<ILawfulBasisSubjectIdExtractor>(),
            options ?? Options.Create(new LawfulBasisOptions()),
            logger ?? new NullLogger<LawfulBasisValidationPipelineBehavior<TestCommand, LanguageExt.Unit>>());
    }
}
