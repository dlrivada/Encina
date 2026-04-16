using Encina.Compliance.Retention;
using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionOptions"/> default values and property behavior.
/// </summary>
public class RetentionOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultRetentionPeriod_ShouldBeNull()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.DefaultRetentionPeriod.ShouldBeNull();
    }

    [Fact]
    public void DefaultAlertBeforeExpirationDays_ShouldBe30()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.AlertBeforeExpirationDays.ShouldBe(30);
    }

    [Fact]
    public void DefaultPublishNotifications_ShouldBeTrue()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.PublishNotifications.ShouldBeTrue();
    }

    [Fact]
    public void DefaultAddHealthCheck_ShouldBeFalse()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void DefaultEnableAutomaticEnforcement_ShouldBeTrue()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.EnableAutomaticEnforcement.ShouldBeTrue();
    }

    [Fact]
    public void DefaultEnforcementInterval_ShouldBe60Minutes()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.EnforcementInterval.ShouldBe(TimeSpan.FromMinutes(60));
    }

    [Fact]
    public void DefaultEnforcementMode_ShouldBeWarn()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.EnforcementMode.ShouldBe(RetentionEnforcementMode.Warn);
    }

    [Fact]
    public void DefaultAutoRegisterFromAttributes_ShouldBeTrue()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.AutoRegisterFromAttributes.ShouldBeTrue();
    }

    [Fact]
    public void DefaultAssembliesToScan_ShouldBeEmpty()
    {
        // Act
        var options = new RetentionOptions();

        // Assert
        options.AssembliesToScan.ShouldBeEmpty();
    }

    #endregion

    #region Property Setter Tests

    [Fact]
    public void SetDefaultRetentionPeriod_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.DefaultRetentionPeriod = TimeSpan.FromDays(365);

        // Assert
        options.DefaultRetentionPeriod.ShouldBe(TimeSpan.FromDays(365));
    }

    [Fact]
    public void SetAlertBeforeExpirationDays_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.AlertBeforeExpirationDays = 14;

        // Assert
        options.AlertBeforeExpirationDays.ShouldBe(14);
    }

    [Fact]
    public void SetPublishNotifications_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.PublishNotifications = false;

        // Assert
        options.PublishNotifications.ShouldBeFalse();
    }

    [Fact]
    public void SetAddHealthCheck_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.AddHealthCheck = true;

        // Assert
        options.AddHealthCheck.ShouldBeTrue();
    }

    [Fact]
    public void SetEnableAutomaticEnforcement_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.EnableAutomaticEnforcement = false;

        // Assert
        options.EnableAutomaticEnforcement.ShouldBeFalse();
    }

    [Fact]
    public void SetEnforcementInterval_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.EnforcementInterval = TimeSpan.FromHours(6);

        // Assert
        options.EnforcementInterval.ShouldBe(TimeSpan.FromHours(6));
    }

    [Fact]
    public void SetEnforcementMode_ToBlock_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.EnforcementMode = RetentionEnforcementMode.Block;

        // Assert
        options.EnforcementMode.ShouldBe(RetentionEnforcementMode.Block);
    }

    [Fact]
    public void SetEnforcementMode_ToDisabled_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.EnforcementMode = RetentionEnforcementMode.Disabled;

        // Assert
        options.EnforcementMode.ShouldBe(RetentionEnforcementMode.Disabled);
    }

    [Fact]
    public void SetAutoRegisterFromAttributes_ShouldUpdateValue()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options.AutoRegisterFromAttributes = false;

        // Assert
        options.AutoRegisterFromAttributes.ShouldBeFalse();
    }

    [Fact]
    public void AssembliesToScan_AddAssembly_ShouldIncludeAssembly()
    {
        // Arrange
        var options = new RetentionOptions();
        var assembly = typeof(RetentionOptions).Assembly;

        // Act
        options.AssembliesToScan.Add(assembly);

        // Assert
        options.AssembliesToScan.ShouldHaveSingleItem()
            .ShouldBeSameAs(assembly);
    }

    #endregion

    #region AddPolicy Tests

    [Fact]
    public void AddPolicy_ShouldReturnSelfForChaining()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        var result = options.AddPolicy("financial-records", policy => policy.RetainForDays(365));

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddPolicy_WithNullDataCategory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        var act = () => options.AddPolicy(null!, policy => policy.RetainForDays(365));

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddPolicy_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        var act = () => options.AddPolicy("financial-records", null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddPolicy_ShouldConfigurePolicyWithBuilder()
    {
        // Arrange
        var options = new RetentionOptions();
        var configureInvoked = false;

        // Act
        options.AddPolicy("financial-records", policy =>
        {
            configureInvoked = true;
            policy.RetainForDays(365);
        });

        // Assert
        configureInvoked.ShouldBeTrue();
    }

    [Fact]
    public void AddPolicy_MultiplePolicies_ShouldAllBeRegistered()
    {
        // Arrange
        var options = new RetentionOptions();

        // Act
        options
            .AddPolicy("financial-records", policy => policy.RetainForYears(7))
            .AddPolicy("session-logs", policy => policy.RetainForDays(90))
            .AddPolicy("marketing-consent", policy => policy.RetainForDays(730));

        // Assert
        options.ConfiguredPolicies.Count.ShouldBe(3);
    }

    #endregion
}
