#pragma warning disable CA2012

using Encina.Caching;
using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Marten;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Services;

public class DefaultApprovedTransferServiceTests
{
    private readonly IAggregateRepository<ApprovedTransferAggregate> _repository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultApprovedTransferService> _logger;
    private readonly DefaultApprovedTransferService _sut;

    public DefaultApprovedTransferServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<ApprovedTransferAggregate>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = TimeProvider.System;
        _logger = NullLogger<DefaultApprovedTransferService>.Instance;

        _sut = new DefaultApprovedTransferService(_repository, _cache, _timeProvider, _logger);
    }

    #region ApproveTransferAsync

    [Fact]
    public async Task ApproveTransferAsync_ValidParams_ReturnsGuid()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ApprovedTransferAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));

        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ApproveTransferAsync(
            "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: id => id.Should().NotBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ApproveTransferAsync_StoreError_ReturnsLeft()
    {
        // Arrange
        var error = EncinaErrors.Create(code: "store.error", message: "Store failed");
        _repository.CreateAsync(Arg.Any<ApprovedTransferAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, LanguageExt.Unit>(error)));

        // Act
        var result = await _sut.ApproveTransferAsync(
            "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveTransferAsync_ArgumentException_ReturnsTransferBlocked()
    {
        // Arrange — The aggregate factory throws ArgumentException for invalid params
        // We trigger this by passing empty approvedBy which causes Approve to throw
        _repository.CreateAsync(Arg.Any<ApprovedTransferAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new ArgumentException("Invalid parameter"));

        // Act
        var result = await _sut.ApproveTransferAsync(
            "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.Should().Contain("blocked", "error should indicate a blocked transfer"));
    }

    #endregion

    #region RevokeTransferAsync

    [Fact]
    public async Task RevokeTransferAsync_ValidTransfer_ReturnsUnit()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            transferId, "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");

        _repository.LoadAsync(transferId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, ApprovedTransferAggregate>(aggregate)));

        _repository.SaveAsync(Arg.Any<ApprovedTransferAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));

        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RevokeTransferAsync(transferId, "No longer needed", "admin");

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeTransferAsync_TransferNotFound_ReturnsLeft()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var error = EncinaErrors.Create(code: "not_found", message: "Not found");

        _repository.LoadAsync(transferId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, ApprovedTransferAggregate>(error)));

        // Act
        var result = await _sut.RevokeTransferAsync(transferId, "reason", "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeTransferAsync_AlreadyRevoked_ReturnsLeft()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            transferId, "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");
        aggregate.Revoke("first revocation", "admin");

        _repository.LoadAsync(transferId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, ApprovedTransferAggregate>(aggregate)));

        // Act — aggregate.Revoke will throw InvalidOperationException, caught by the service
        var result = await _sut.RevokeTransferAsync(transferId, "second revocation", "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.Should().Contain("revoked", "error should indicate already revoked"));
    }

    #endregion

    #region RenewTransferAsync

    [Fact]
    public async Task RenewTransferAsync_ValidTransfer_ReturnsUnit()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            transferId, "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");

        _repository.LoadAsync(transferId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, ApprovedTransferAggregate>(aggregate)));

        _repository.SaveAsync(Arg.Any<ApprovedTransferAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));

        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var newExpiry = DateTimeOffset.UtcNow.AddYears(1);

        // Act
        var result = await _sut.RenewTransferAsync(transferId, newExpiry, "admin");

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RenewTransferAsync_RevokedTransfer_ReturnsLeft()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            transferId, "DE", "US", "personal-data", TransferBasis.SCCs, approvedBy: "admin");
        aggregate.Revoke("revoked", "admin");

        _repository.LoadAsync(transferId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, ApprovedTransferAggregate>(aggregate)));

        var newExpiry = DateTimeOffset.UtcNow.AddYears(1);

        // Act — aggregate.Renew will throw InvalidOperationException, caught by the service
        var result = await _sut.RenewTransferAsync(transferId, newExpiry, "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.Should().Contain("revoked", "error should indicate already revoked"));
    }

    #endregion

    #region IsTransferApprovedAsync

    [Fact]
    public async Task IsTransferApprovedAsync_NoTransferFound_ReturnsFalse()
    {
        // Arrange — cache miss returns null, GetApprovedTransferAsync returns Left (not found).
        // IsTransferApprovedAsync converts the not-found Left to Right(false).
        _cache.GetAsync<ApprovedTransferReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ApprovedTransferReadModel?>(null));

        // Act
        var result = await _sut.IsTransferApprovedAsync("DE", "US", "personal-data");

        // Assert — not-found is treated as "not approved" (Right: false)
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: isApproved => isApproved.Should().BeFalse(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetApprovedTransferAsync

    [Fact]
    public async Task GetApprovedTransferAsync_CacheHit_ReturnsCachedModel()
    {
        // Arrange
        var readModel = new ApprovedTransferReadModel
        {
            Id = Guid.NewGuid(),
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "personal-data",
            Basis = TransferBasis.SCCs,
            ApprovedBy = "admin",
            IsRevoked = false,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1)
        };

        _cache.GetAsync<ApprovedTransferReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ApprovedTransferReadModel?>(readModel));

        // Act
        var result = await _sut.GetApprovedTransferAsync("DE", "US", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: model =>
            {
                model.Id.Should().Be(readModel.Id);
                return model;
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetApprovedTransferAsync_CacheMiss_ReturnsNotFoundLeft()
    {
        // Arrange — cache miss returns null, service returns a not-found Left error.
        _cache.GetAsync<ApprovedTransferReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ApprovedTransferReadModel?>(null));

        // Act
        var result = await _sut.GetApprovedTransferAsync("DE", "US", "personal-data");

        // Assert — returns Left with transfer_not_found error code
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.Should().Contain("No approved transfer found", "error should indicate not found"));
    }

    #endregion
}
