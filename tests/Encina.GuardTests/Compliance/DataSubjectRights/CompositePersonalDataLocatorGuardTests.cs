using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="CompositePersonalDataLocator"/> verifying null/whitespace parameter handling.
/// </summary>
public class CompositePersonalDataLocatorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullLocators_ThrowsArgumentNullException()
    {
        var act = () => new CompositePersonalDataLocator(
            null!,
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("locators");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var locators = Array.Empty<IPersonalDataLocator>();

        var act = () => new CompositePersonalDataLocator(locators, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region LocateAllDataAsync Guards

    [Fact]
    public async Task LocateAllDataAsync_NullSubjectId_ThrowsArgumentException()
    {
        var sut = new CompositePersonalDataLocator(
            Array.Empty<IPersonalDataLocator>(),
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        var act = () => sut.LocateAllDataAsync(null!).AsTask();

        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task LocateAllDataAsync_EmptySubjectId_ThrowsArgumentException()
    {
        var sut = new CompositePersonalDataLocator(
            Array.Empty<IPersonalDataLocator>(),
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        var act = () => sut.LocateAllDataAsync("").AsTask();

        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task LocateAllDataAsync_WhitespaceSubjectId_ThrowsArgumentException()
    {
        var sut = new CompositePersonalDataLocator(
            Array.Empty<IPersonalDataLocator>(),
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        var act = () => sut.LocateAllDataAsync("   ").AsTask();

        await Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion
}
