using Encina.Testing.Bogus;

namespace Encina.Quartz.Tests.Fakers;

/// <summary>
/// Faker for generating test notification data used in Quartz notification job tests.
/// </summary>
public sealed class TestNotificationFaker : EncinaFaker<TestNotification>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestNotificationFaker"/> class.
    /// </summary>
    public TestNotificationFaker()
    {
        CustomInstantiator(f => new TestNotification(f.Lorem.Sentence()));
    }

    /// <summary>
    /// Configures the faker to generate notifications with specific message.
    /// </summary>
    /// <param name="message">The message to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestNotificationFaker WithMessage(string message)
    {
        CustomInstantiator(_ => new TestNotification(message));
        return this;
    }
}

/// <summary>
/// Test notification type used in Quartz notification job tests.
/// </summary>
/// <param name="Message">The test message.</param>
public sealed record TestNotification(string Message) : INotification;
