namespace Encina.Caching.GuardTests;

/// <summary>
/// Guard tests for <see cref="DefaultCacheKeyGenerator"/> to verify null parameter handling.
/// </summary>
public class DefaultCacheKeyGeneratorGuardTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        IOptions<CachingOptions> options = null!;

        // Act & Assert
        var act = () => new DefaultCacheKeyGenerator(options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that GenerateKey throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public void GenerateKey_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { KeyPrefix = "test" });
        var generator = new DefaultCacheKeyGenerator(options);
        var context = CreateContext();
        TestQuery request = null!;

        // Act & Assert
        var act = () => generator.GenerateKey<TestQuery, string>(request, context);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that GenerateKey throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void GenerateKey_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { KeyPrefix = "test" });
        var generator = new DefaultCacheKeyGenerator(options);
        var request = new TestQuery(1);
        IRequestContext context = null!;

        // Act & Assert
        var act = () => generator.GenerateKey<TestQuery, string>(request, context);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that GeneratePattern throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void GeneratePattern_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { KeyPrefix = "test" });
        var generator = new DefaultCacheKeyGenerator(options);
        IRequestContext context = null!;

        // Act & Assert
        var act = () => generator.GeneratePattern<TestQuery>(context);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that GeneratePatternFromTemplate throws ArgumentNullException when keyTemplate is null.
    /// </summary>
    [Fact]
    public void GeneratePatternFromTemplate_NullKeyTemplate_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { KeyPrefix = "test" });
        var generator = new DefaultCacheKeyGenerator(options);
        var request = new TestQuery(1);
        var context = CreateContext();
        string keyTemplate = null!;

        // Act & Assert
        var act = () => generator.GeneratePatternFromTemplate(keyTemplate, request, context);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyTemplate");
    }

    /// <summary>
    /// Verifies that GeneratePatternFromTemplate throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void GeneratePatternFromTemplate_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { KeyPrefix = "test" });
        var generator = new DefaultCacheKeyGenerator(options);
        var request = new TestQuery(1);
        IRequestContext context = null!;
        var keyTemplate = "test:{Id}";

        // Act & Assert
        var act = () => generator.GeneratePatternFromTemplate(keyTemplate, request, context);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    private static IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns("tenant1");
        context.UserId.Returns("user1");
        context.CorrelationId.Returns("corr-123");
        return context;
    }

    [Cache(DurationSeconds = 300)]
    private sealed record TestQuery(int Id) : IRequest<string>;
}
