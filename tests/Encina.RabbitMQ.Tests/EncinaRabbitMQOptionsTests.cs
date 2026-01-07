namespace Encina.RabbitMQ.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaRabbitMQOptions"/>.
/// </summary>
public sealed class EncinaRabbitMQOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EncinaRabbitMQOptions();

        // Assert
        options.HostName.ShouldBe("localhost");
        options.Port.ShouldBe(5672);
        options.VirtualHost.ShouldBe("/");
        options.UserName.ShouldBe("guest");
        options.Password.ShouldBe("guest");
        options.ExchangeName.ShouldBe("encina");
        options.UsePublisherConfirms.ShouldBeTrue();
        options.PrefetchCount.ShouldBe((ushort)10);
        options.Durable.ShouldBeTrue();
    }

    [Fact]
    public void HostName_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.HostName = "custom-host";

        // Assert
        options.HostName.ShouldBe("custom-host");
    }

    [Fact]
    public void Port_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.Port = 5673;

        // Assert
        options.Port.ShouldBe(5673);
    }

    [Fact]
    public void VirtualHost_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.VirtualHost = "/custom";

        // Assert
        options.VirtualHost.ShouldBe("/custom");
    }

    [Fact]
    public void UserName_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.UserName = "admin";

        // Assert
        options.UserName.ShouldBe("admin");
    }

    [Fact]
    public void Password_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.Password = "secret";

        // Assert
        options.Password.ShouldBe("secret");
    }

    [Fact]
    public void ExchangeName_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.ExchangeName = "custom-exchange";

        // Assert
        options.ExchangeName.ShouldBe("custom-exchange");
    }

    [Fact]
    public void UsePublisherConfirms_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.UsePublisherConfirms = false;

        // Assert
        options.UsePublisherConfirms.ShouldBeFalse();
    }

    [Fact]
    public void PrefetchCount_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.PrefetchCount = 50;

        // Assert
        options.PrefetchCount.ShouldBe((ushort)50);
    }

    [Fact]
    public void Durable_CanBeSet()
    {
        // Arrange
        var options = new EncinaRabbitMQOptions();

        // Act
        options.Durable = false;

        // Assert
        options.Durable.ShouldBeFalse();
    }

    [Fact]
    public void ProviderHealthCheck_IsNotNull()
    {
        // Arrange & Act
        var options = new EncinaRabbitMQOptions();

        // Assert
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_DefaultEnabled_IsTrue()
    {
        // Arrange & Act
        var options = new EncinaRabbitMQOptions();

        // Assert - opt-out design means health checks are enabled by default
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
    }
}
