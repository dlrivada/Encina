using Encina.Testing.Bogus;

namespace Encina.Hangfire.Tests.Fakers;

/// <summary>
/// Faker for generating test notification data used in Hangfire notification job adapter tests.
/// </summary>
public sealed class TestNotificationFaker : EncinaFaker<TestNotificationData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestNotificationFaker"/> class.
    /// </summary>
    public TestNotificationFaker()
    {
        CustomInstantiator(f => new TestNotificationData(f.Lorem.Sentence()));
    }

    /// <summary>
    /// Configures the faker to generate notifications with specific message.
    /// </summary>
    /// <param name="message">The message to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestNotificationFaker WithMessage(string message)
    {
        CustomInstantiator(_ => new TestNotificationData(message));
        return this;
    }
}

/// <summary>
/// Test notification data type used in Hangfire notification job adapter tests.
/// </summary>
/// <param name="Message">The test message.</param>
public sealed record TestNotificationData(string Message) : INotification;
