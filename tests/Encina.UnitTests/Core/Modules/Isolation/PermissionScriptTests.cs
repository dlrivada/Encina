using Encina.Modules.Isolation;

namespace Encina.UnitTests.Core.Modules.Isolation;

/// <summary>
/// Unit tests for <see cref="PermissionScript"/> record struct.
/// </summary>
public class PermissionScriptTests
{
    #region ForSchemaCreation

    [Fact]
    public void ForSchemaCreation_ShouldCreateCorrectScript()
    {
        // Arrange
        const string content = "CREATE SCHEMA test;";

        // Act
        var script = PermissionScript.ForSchemaCreation(content);

        // Assert
        script.Name.ShouldBe("001_create_schemas.sql");
        script.Description.ShouldBe("Creates database schemas for modules");
        script.Content.ShouldBe(content);
        script.Order.ShouldBe(1);
    }

    #endregion

    #region ForUserCreation

    [Fact]
    public void ForUserCreation_ShouldCreateCorrectScript()
    {
        // Arrange
        const string content = "CREATE USER test;";

        // Act
        var script = PermissionScript.ForUserCreation(content);

        // Assert
        script.Name.ShouldBe("002_create_users.sql");
        script.Description.ShouldBe("Creates database users/logins for modules");
        script.Content.ShouldBe(content);
        script.Order.ShouldBe(2);
    }

    #endregion

    #region ForGrantPermissions

    [Fact]
    public void ForGrantPermissions_ShouldCreateCorrectScript()
    {
        // Arrange
        const string content = "GRANT SELECT ON SCHEMA test;";

        // Act
        var script = PermissionScript.ForGrantPermissions(content);

        // Assert
        script.Name.ShouldBe("003_grant_permissions.sql");
        script.Description.ShouldBe("Grants schema permissions to module users");
        script.Content.ShouldBe(content);
        script.Order.ShouldBe(3);
    }

    #endregion

    #region ForRevokePermissions

    [Fact]
    public void ForRevokePermissions_ShouldCreateCorrectScript()
    {
        // Arrange
        const string content = "REVOKE ALL ON SCHEMA test;";

        // Act
        var script = PermissionScript.ForRevokePermissions(content);

        // Assert
        script.Name.ShouldBe("000_revoke_permissions.sql");
        script.Description.ShouldBe("Revokes all module permissions (cleanup)");
        script.Content.ShouldBe(content);
        script.Order.ShouldBe(0);
    }

    #endregion

    #region ForModule

    [Fact]
    public void ForModule_ShouldCreateCorrectScript()
    {
        // Arrange
        const string moduleName = "Orders";
        const string content = "-- Module permissions script";

        // Act
        var script = PermissionScript.ForModule(moduleName, content);

        // Assert
        script.Name.ShouldBe("module_orders_permissions.sql");
        script.Description.ShouldBe("Permissions for Orders module");
        script.Content.ShouldBe(content);
        script.Order.ShouldBe(10);
    }

    [Fact]
    public void ForModule_MixedCaseModuleName_ShouldLowercaseInFileName()
    {
        // Arrange
        const string moduleName = "PaymentGateway";
        const string content = "-- Script content";

        // Act
        var script = PermissionScript.ForModule(moduleName, content);

        // Assert
        script.Name.ShouldBe("module_paymentgateway_permissions.sql");
        script.Description.ShouldBe("Permissions for PaymentGateway module");
    }

    #endregion

    #region Record Equality

    [Fact]
    public void PermissionScript_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var script1 = new PermissionScript("test.sql", "Test script", "content", 1);
        var script2 = new PermissionScript("test.sql", "Test script", "content", 1);

        // Assert
        script1.ShouldBe(script2);
        (script1 == script2).ShouldBeTrue();
    }

    [Fact]
    public void PermissionScript_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var script1 = new PermissionScript("test1.sql", "Test script", "content", 1);
        var script2 = new PermissionScript("test2.sql", "Test script", "content", 1);

        // Assert
        script1.ShouldNotBe(script2);
        (script1 != script2).ShouldBeTrue();
    }

    #endregion

    #region Execution Order

    [Fact]
    public void Scripts_ShouldHaveCorrectExecutionOrder()
    {
        // Arrange
        var revoke = PermissionScript.ForRevokePermissions("revoke");
        var schemas = PermissionScript.ForSchemaCreation("schemas");
        var users = PermissionScript.ForUserCreation("users");
        var grants = PermissionScript.ForGrantPermissions("grants");
        var module = PermissionScript.ForModule("Test", "module");

        var scripts = new[] { grants, users, module, revoke, schemas };

        // Act
        var orderedScripts = scripts.OrderBy(s => s.Order).ToList();

        // Assert
        orderedScripts[0].ShouldBe(revoke);   // Order 0
        orderedScripts[1].ShouldBe(schemas);  // Order 1
        orderedScripts[2].ShouldBe(users);    // Order 2
        orderedScripts[3].ShouldBe(grants);   // Order 3
        orderedScripts[4].ShouldBe(module);   // Order 10
    }

    #endregion
}
