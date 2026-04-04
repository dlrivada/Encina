using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Deep method-level guard tests for <see cref="CompositePersonalDataLocator"/>.
/// </summary>
public class CompositePersonalDataLocatorMethodGuardTests
{
    private readonly CompositePersonalDataLocator _sut;

    public CompositePersonalDataLocatorMethodGuardTests()
    {
        _sut = new CompositePersonalDataLocator(
            Enumerable.Empty<IPersonalDataLocator>(),
            NullLogger<CompositePersonalDataLocator>.Instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LocateAllDataAsync_NullOrWhitespaceSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = () => _sut.LocateAllDataAsync(subjectId!).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }
}
