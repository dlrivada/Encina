using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="HardDeleteErasureStrategy"/> verifying erasure behavior.
/// </summary>
public class HardDeleteErasureStrategyTests
{
    private readonly HardDeleteErasureStrategy _sut = new(
        NullLoggerFactory.Instance.CreateLogger<HardDeleteErasureStrategy>());

    [Fact]
    public async Task EraseFieldAsync_ValidLocation_ReturnsSuccess()
    {
        var location = new PersonalDataLocation
        {
            EntityType = typeof(object),
            EntityId = "entity-1",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = true,
            HasLegalRetention = false,
            CurrentValue = "test@example.com"
        };

        var result = await _sut.EraseFieldAsync(location);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task EraseFieldAsync_LocationWithNullValue_ReturnsSuccess()
    {
        var location = new PersonalDataLocation
        {
            EntityType = typeof(object),
            EntityId = "entity-1",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = true,
            HasLegalRetention = false,
            CurrentValue = null
        };

        var result = await _sut.EraseFieldAsync(location);

        result.IsRight.ShouldBeTrue();
    }
}
