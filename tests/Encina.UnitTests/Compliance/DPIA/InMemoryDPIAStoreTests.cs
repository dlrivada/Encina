#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="InMemoryDPIAStore"/>.
/// </summary>
public class InMemoryDPIAStoreTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryDPIAStore _sut = new(NullLogger<InMemoryDPIAStore>.Instance);

    private static DPIAAssessment CreateAssessment(
        string requestTypeName = "Ns.TestCommand",
        DPIAAssessmentStatus status = DPIAAssessmentStatus.Draft,
        DateTimeOffset? nextReviewAtUtc = null) => new()
        {
            Id = Guid.NewGuid(),
            RequestTypeName = requestTypeName,
            Status = status,
            CreatedAtUtc = FixedNow,
            NextReviewAtUtc = nextReviewAtUtc,
        };

    #region SaveAssessmentAsync Tests

    [Fact]
    public async Task SaveAssessmentAsync_ValidAssessment_ReturnsUnit()
    {
        var assessment = CreateAssessment();

        var result = await _sut.SaveAssessmentAsync(assessment);

        result.IsRight.Should().BeTrue();
        _sut.Count.Should().Be(1);
    }

    [Fact]
    public async Task SaveAssessmentAsync_SameRequestType_OverwritesPrevious()
    {
        var first = CreateAssessment("Ns.TestCommand", DPIAAssessmentStatus.Draft);
        var second = CreateAssessment("Ns.TestCommand", DPIAAssessmentStatus.Approved);

        await _sut.SaveAssessmentAsync(first);
        await _sut.SaveAssessmentAsync(second);

        _sut.Count.Should().Be(2); // Both stored by Id
        var result = await _sut.GetAssessmentAsync("Ns.TestCommand");
        _ = result.Match(
            Right: opt => opt.Match(
                Some: a => a.Status.Should().Be(DPIAAssessmentStatus.Approved),
                None: () => throw new InvalidOperationException("Expected Some")),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetAssessmentAsync Tests

    [Fact]
    public async Task GetAssessmentAsync_ExistingType_ReturnsAssessment()
    {
        var assessment = CreateAssessment();
        await _sut.SaveAssessmentAsync(assessment);

        var result = await _sut.GetAssessmentAsync(assessment.RequestTypeName);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: opt => opt.IsSome.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAssessmentAsync_NonExistingType_ReturnsNull()
    {
        var result = await _sut.GetAssessmentAsync("Ns.NonExistent");

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: opt => opt.IsNone.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetAssessmentByIdAsync Tests

    [Fact]
    public async Task GetAssessmentByIdAsync_ExistingId_ReturnsAssessment()
    {
        var assessment = CreateAssessment();
        await _sut.SaveAssessmentAsync(assessment);

        var result = await _sut.GetAssessmentByIdAsync(assessment.Id);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: opt => opt.Match(
                Some: a => a.Id.Should().Be(assessment.Id),
                None: () => throw new InvalidOperationException("Expected Some")),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAssessmentByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sut.GetAssessmentByIdAsync(Guid.NewGuid());

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: opt => opt.IsNone.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetExpiredAssessmentsAsync Tests

    [Fact]
    public async Task GetExpiredAssessmentsAsync_WithExpired_ReturnsThem()
    {
        var expired = CreateAssessment(
            "Ns.Expired", DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: FixedNow.AddDays(-1));
        var current = CreateAssessment(
            "Ns.Current", DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: FixedNow.AddDays(30));

        await _sut.SaveAssessmentAsync(expired);
        await _sut.SaveAssessmentAsync(current);

        var result = await _sut.GetExpiredAssessmentsAsync(FixedNow);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: list => list.Should().HaveCount(1),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetExpiredAssessmentsAsync_NoExpired_ReturnsEmpty()
    {
        var result = await _sut.GetExpiredAssessmentsAsync(FixedNow);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: list => list.Should().BeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetExpiredAssessmentsAsync_DraftWithPastReview_NotReturned()
    {
        // Draft assessments should NOT be returned as expired
        var draft = CreateAssessment(
            "Ns.Draft", DPIAAssessmentStatus.Draft,
            nextReviewAtUtc: FixedNow.AddDays(-1));
        await _sut.SaveAssessmentAsync(draft);

        var result = await _sut.GetExpiredAssessmentsAsync(FixedNow);

        _ = result.Match(
            Right: list => list.Should().BeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetAllAssessmentsAsync Tests

    [Fact]
    public async Task GetAllAssessmentsAsync_ReturnsAll()
    {
        await _sut.SaveAssessmentAsync(CreateAssessment("Ns.A"));
        await _sut.SaveAssessmentAsync(CreateAssessment("Ns.B"));

        var result = await _sut.GetAllAssessmentsAsync();

        _ = result.Match(
            Right: list => list.Should().HaveCount(2),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region DeleteAssessmentAsync Tests

    [Fact]
    public async Task DeleteAssessmentAsync_ExistingId_Removes()
    {
        var assessment = CreateAssessment();
        await _sut.SaveAssessmentAsync(assessment);

        var result = await _sut.DeleteAssessmentAsync(assessment.Id);

        result.IsRight.Should().BeTrue();
        _sut.Count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAssessmentAsync_NonExistingId_ReturnsError()
    {
        var result = await _sut.DeleteAssessmentAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAssessmentAsync_RemovesBothIndices()
    {
        var assessment = CreateAssessment();
        await _sut.SaveAssessmentAsync(assessment);
        await _sut.DeleteAssessmentAsync(assessment.Id);

        var byType = await _sut.GetAssessmentAsync(assessment.RequestTypeName);
        var byId = await _sut.GetAssessmentByIdAsync(assessment.Id);

        _ = byType.Match(Right: opt => opt.IsNone.Should().BeTrue(), Left: _ => { });
        _ = byId.Match(Right: opt => opt.IsNone.Should().BeTrue(), Left: _ => { });
    }

    #endregion

    #region Clear and Count Tests

    [Fact]
    public async Task Clear_RemovesAllEntries()
    {
        await _sut.SaveAssessmentAsync(CreateAssessment("Ns.A"));
        await _sut.SaveAssessmentAsync(CreateAssessment("Ns.B"));

        _sut.Clear();

        _sut.Count.Should().Be(0);
    }

    #endregion
}
