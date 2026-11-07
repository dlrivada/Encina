using Encina.Testing.Testcontainers;

using Shouldly;

using Testcontainers.MySql;

namespace Encina.UnitTests.Testing.Testcontainers;

/// <summary>
/// Unit tests proving that every MySQL entry point of Encina.Testing.Testcontainers reads the single
/// default image, WITHOUT starting Docker containers.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ContainerDefaultImagesTests
{
    [Fact]
    public void MySql_Constant_IsTheVendorLtsLine()
    {
        ContainerDefaultImages.MySql.ShouldBe("mysql:8.4");
    }

    [Fact]
    public void MySql_ConfigureOverload_UsesTheConstant()
    {
        var fixture = EncinaContainers.MySql(_ => { });

        fixture.Container.Image.FullName.ShouldBe(ContainerDefaultImages.MySql);
    }

    [Fact]
    public void MySql_ParameterlessOverload_ReturnsTheFixtureThatUsesTheConstant()
    {
        var fixture = EncinaContainers.MySql();

        fixture.ShouldBeOfType<MySqlContainerFixture>();
        new BuildExposingMySqlFixture().Build().Image.FullName.ShouldBe(ContainerDefaultImages.MySql);
    }

    [Fact]
    public void MySqlContainerFixture_BuildContainer_UsesTheConstant()
    {
        var container = new BuildExposingMySqlFixture().Build();

        container.Image.FullName.ShouldBe(ContainerDefaultImages.MySql);
    }

    private sealed class BuildExposingMySqlFixture : MySqlContainerFixture
    {
        public MySqlContainer Build() => BuildContainer();
    }
}
