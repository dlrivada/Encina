#pragma warning disable CA2012

using Encina.Compliance.DataSubjectRights;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="CompositePersonalDataLocator"/> verifying aggregation of
/// multiple locator results and partial failure handling.
/// </summary>
public class CompositePersonalDataLocatorTests
{
    private static PersonalDataLocation CreateLocation(string fieldName, string entityId = "entity-1") => new()
    {
        EntityType = typeof(object),
        EntityId = entityId,
        FieldName = fieldName,
        Category = PersonalDataCategory.Identity,
        IsErasable = true,
        IsPortable = true,
        HasLegalRetention = false,
        CurrentValue = "value"
    };

    private static IPersonalDataLocator CreateSuccessLocator(params PersonalDataLocation[] locations)
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                IReadOnlyList<PersonalDataLocation> list = locations.ToList();
                return new ValueTask<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(
                    Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(list));
            });
        return locator;
    }

    private static IPersonalDataLocator CreateFailLocator(string errorMessage)
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
                new ValueTask<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(
                    Left<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                        DSRErrors.LocatorFailed("subject", errorMessage))));
        return locator;
    }

    [Fact]
    public async Task LocateAllDataAsync_NoLocators_ReturnsEmptyList()
    {
        var emptyLocators = System.Array.Empty<IPersonalDataLocator>();
        var sut = new CompositePersonalDataLocator(
            emptyLocators,
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        var result = await sut.LocateAllDataAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.Count.ShouldBe(0);
    }

    [Fact]
    public async Task LocateAllDataAsync_SingleLocator_ReturnsItsResults()
    {
        var locator = CreateSuccessLocator(CreateLocation("Email"), CreateLocation("Name"));

        var sut = new CompositePersonalDataLocator(
            [locator],
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        var result = await sut.LocateAllDataAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.Count.ShouldBe(2);
    }

    [Fact]
    public async Task LocateAllDataAsync_MultipleLocators_AggregatesResults()
    {
        var locator1 = CreateSuccessLocator(CreateLocation("Email"));
        var locator2 = CreateSuccessLocator(CreateLocation("Phone"));

        var sut = new CompositePersonalDataLocator(
            [locator1, locator2],
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        var result = await sut.LocateAllDataAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.Count.ShouldBe(2);
    }

    [Fact]
    public async Task LocateAllDataAsync_PartialFailure_ReturnsSuccessfulResults()
    {
        var locator1 = CreateSuccessLocator(CreateLocation("Email"));
        var locator2 = CreateFailLocator("Database unavailable");

        var sut = new CompositePersonalDataLocator(
            [locator1, locator2],
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        var result = await sut.LocateAllDataAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        var data = result.RightAsEnumerable().First();
        data.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LocateAllDataAsync_AllLocatorsFail_ReturnsError()
    {
        var locator1 = CreateFailLocator("Error 1");
        var locator2 = CreateFailLocator("Error 2");

        var sut = new CompositePersonalDataLocator(
            [locator1, locator2],
            NullLoggerFactory.Instance.CreateLogger<CompositePersonalDataLocator>());

        var result = await sut.LocateAllDataAsync("subject-1");

        result.IsLeft.ShouldBeTrue();
    }
}
