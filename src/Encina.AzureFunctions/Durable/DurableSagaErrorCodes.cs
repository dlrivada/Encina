namespace Encina.AzureFunctions.Durable;

/// <summary>
/// Error codes for durable saga builder operations.
/// </summary>
public static class DurableSagaErrorCodes
{
    /// <summary>
    /// No steps defined in the durable saga definition.
    /// </summary>
    public const string NoSteps = "durable_saga.no_steps";

    /// <summary>
    /// A durable saga step was not configured with an execute activity.
    /// </summary>
    public const string StepNotConfigured = "durable_saga.step_not_configured";
}
