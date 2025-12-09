namespace SimpleMediator;

internal static class MediatorBehaviorGuards
{
    public static bool TryValidateRequest(Type behaviorType, object? request, out MediatorError failure)
    {
        if (request is not null)
        {
            failure = default;
            return true;
        }

        var message = $"{behaviorType.Name} received a null request.";
        failure = MediatorErrors.Create("mediator.behavior.null_request", message);
        return false;
    }

    public static bool TryValidateNextStep(Type behaviorType, Delegate? nextStep, out MediatorError failure)
    {
        if (nextStep is not null)
        {
            failure = default;
            return true;
        }

        var message = $"{behaviorType.Name} received a null callback.";
        failure = MediatorErrors.Create("mediator.behavior.null_next", message);
        return false;
    }
}
