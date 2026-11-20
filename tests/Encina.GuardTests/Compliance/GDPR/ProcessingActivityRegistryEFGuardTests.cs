using Encina.Compliance.GDPR;
using Microsoft.EntityFrameworkCore;
using EFCorePA = Encina.EntityFrameworkCore.ProcessingActivity;

namespace Encina.GuardTests.Compliance.GDPR;

/// <summary>
/// Guard tests for the member arguments of <see cref="EFCorePA.ProcessingActivityRegistryEF"/>.
/// </summary>
public sealed class ProcessingActivityRegistryEFGuardTests : IDisposable
{
    private readonly DbContext _dbContext = new(new DbContextOptionsBuilder<DbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private EFCorePA.ProcessingActivityRegistryEF CreateSut() => new(_dbContext);

    [Fact]
    public async Task RegisterActivityAsync_NullActivity_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.RegisterActivityAsync(null!));

        ex.ParamName.ShouldBe("activity");
    }

    [Fact]
    public async Task UpdateActivityAsync_NullActivity_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.UpdateActivityAsync(null!));

        ex.ParamName.ShouldBe("activity");
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NullRequestType_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.GetActivityByRequestTypeAsync(null!));

        ex.ParamName.ShouldBe("requestType");
    }

    public void Dispose() => _dbContext.Dispose();
}
