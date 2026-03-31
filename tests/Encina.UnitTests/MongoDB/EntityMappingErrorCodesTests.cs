using Encina.MongoDB;

namespace Encina.UnitTests.MongoDB;

public sealed class EntityMappingErrorCodesTests
{
    [Theory]
    [InlineData(nameof(EntityMappingErrorCodes.MissingTableName))]
    [InlineData(nameof(EntityMappingErrorCodes.MissingPrimaryKey))]
    [InlineData(nameof(EntityMappingErrorCodes.MissingColumnMappings))]
    [InlineData(nameof(EntityMappingErrorCodes.MissingTenantColumn))]
    public void AllCodes_StartWithMongodbMapping(string fieldName)
    {
        var value = typeof(EntityMappingErrorCodes).GetField(fieldName)?.GetValue(null) as string;
        value.ShouldNotBeNull();
        value.ShouldStartWith("mongodb.mapping.");
    }

    [Fact]
    public void AllCodes_AreUnique()
    {
        var codes = new[]
        {
            EntityMappingErrorCodes.MissingTableName,
            EntityMappingErrorCodes.MissingPrimaryKey,
            EntityMappingErrorCodes.MissingColumnMappings,
            EntityMappingErrorCodes.MissingTenantColumn
        };
        codes.Distinct().Count().ShouldBe(codes.Length);
    }
}
