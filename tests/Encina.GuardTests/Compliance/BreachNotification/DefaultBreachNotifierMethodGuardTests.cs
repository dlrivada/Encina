#pragma warning disable CA2012 // ValueTask consumed correctly in test assertions

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="DefaultBreachNotifier"/> method-level parameter validation.
/// Constructor guards are in <see cref="DefaultBreachNotifierGuardTests"/>.
/// </summary>
public class DefaultBreachNotifierMethodGuardTests
{
    private readonly DefaultBreachNotifier _sut = new(TimeProvider.System, NullLogger<DefaultBreachNotifier>.Instance);

    [Fact]
    public async Task NotifyAuthorityAsync_NullBreach_ThrowsArgumentNullException()
    {
        var act = () => _sut.NotifyAuthorityAsync(null!).AsTask();

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("breach");
    }

    [Fact]
    public async Task NotifyDataSubjectsAsync_NullBreach_ThrowsArgumentNullException()
    {
        var act = () => _sut.NotifyDataSubjectsAsync(null!, ["subject-1"]).AsTask();

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("breach");
    }

    [Fact]
    public async Task NotifyDataSubjectsAsync_NullSubjectIds_ThrowsArgumentNullException()
    {
        var breach = BreachRecord.Create(
            nature: "test",
            approximateSubjectsAffected: 1,
            categoriesOfDataAffected: ["email"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Risk",
            measuresTaken: "Measures",
            detectedAtUtc: DateTimeOffset.UtcNow,
            severity: BreachSeverity.Medium);

        var act = () => _sut.NotifyDataSubjectsAsync(breach, null!).AsTask();

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("subjectIds");
    }
}
