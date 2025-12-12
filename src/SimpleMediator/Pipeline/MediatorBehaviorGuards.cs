using System.Collections.Generic;

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
        var metadata = new Dictionary<string, object?>
        {
            ["behavior"] = behaviorType.FullName,
            ["stage"] = "behavior_guard",
            ["issue"] = "null_request"
        };
        failure = MediatorErrors.Create(MediatorErrorCodes.BehaviorNullRequest, message, details: metadata);
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
        var metadata = new Dictionary<string, object?>
        {
            ["behavior"] = behaviorType.FullName,
            ["stage"] = "behavior_guard",
            ["issue"] = "null_next"
        };
        failure = MediatorErrors.Create(MediatorErrorCodes.BehaviorNullNext, message, details: metadata);
        return false;
    }
}
