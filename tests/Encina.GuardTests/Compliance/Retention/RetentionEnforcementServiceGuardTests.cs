using Encina.Compliance.Retention;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionEnforcementService"/> constructor null parameter handling.
/// </summary>
public sealed class RetentionEnforcementServiceGuardTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IOptions<RetentionOptions> _options = Options.Create(new RetentionOptions());
    private readonly ILogger<RetentionEnforcementService> _logger = NullLogger<RetentionEnforcementService>.Instance;

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new RetentionEnforcementService(null!, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new RetentionEnforcementService(_scopeFactory, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RetentionEnforcementService(_scopeFactory, _options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
