using Encina.Marten.Versioning;

namespace Encina.UnitTests.Marten.Versioning;

public sealed class EventVersioningErrorCodesTests
{
    [Theory]
    [InlineData(nameof(EventVersioningErrorCodes.UpcastFailed))]
    [InlineData(nameof(EventVersioningErrorCodes.UpcasterNotFound))]
    [InlineData(nameof(EventVersioningErrorCodes.RegistrationFailed))]
    [InlineData(nameof(EventVersioningErrorCodes.DuplicateUpcaster))]
    [InlineData(nameof(EventVersioningErrorCodes.InvalidConfiguration))]
    public void AllCodes_ShouldStartWithVersioningPrefix(string fieldName)
    {
        var value = typeof(EventVersioningErrorCodes).GetField(fieldName)?.GetValue(null) as string;
        value.ShouldNotBeNull();
        value.ShouldStartWith("event.versioning.");
    }

    [Fact]
    public void AllCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            EventVersioningErrorCodes.UpcastFailed,
            EventVersioningErrorCodes.UpcasterNotFound,
            EventVersioningErrorCodes.RegistrationFailed,
            EventVersioningErrorCodes.DuplicateUpcaster,
            EventVersioningErrorCodes.InvalidConfiguration
        };
        codes.Distinct().Count().ShouldBe(codes.Length);
    }
}
