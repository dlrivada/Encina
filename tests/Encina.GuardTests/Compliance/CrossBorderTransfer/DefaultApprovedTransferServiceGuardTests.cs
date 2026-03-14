#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Marten;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="DefaultApprovedTransferService"/> to verify null parameter handling.
/// </summary>
public class DefaultApprovedTransferServiceGuardTests
{
    private readonly IAggregateRepository<ApprovedTransferAggregate> _repository = Substitute.For<IAggregateRepository<ApprovedTransferAggregate>>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultApprovedTransferService> _logger = NullLogger<DefaultApprovedTransferService>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when repository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultApprovedTransferService(
            null!, _cache, _timeProvider, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("repository");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultApprovedTransferService(
            _repository, null!, _timeProvider, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultApprovedTransferService(
            _repository, _cache, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultApprovedTransferService(
            _repository, _cache, _timeProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
