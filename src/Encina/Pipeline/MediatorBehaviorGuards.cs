using System.Collections.Generic;

namespace Encina;

internal static class EncinaBehaviorGuards
{
    public static bool TryValidateRequest(Type behaviorType, object? request, out EncinaError failure)
    {
        if (request is not null)
        {
            failure = default;
            return true;
        }

        string message = $"{behaviorType.Name} received a null request.";
        var metadata = new Dictionary<string, object?>
        {
            ["behavior"] = behaviorType.FullName,
            ["stage"] = "behavior_guard",
            ["issue"] = "null_request"
        };
        failure = EncinaErrors.Create(EncinaErrorCodes.BehaviorNullRequest, message, details: metadata);
        return false;
    }

    public static bool TryValidateNextStep(Type behaviorType, Delegate? nextStep, out EncinaError failure)
    {
        if (nextStep is not null)
        {
            failure = default;
            return true;
        }

        string message = $"{behaviorType.Name} received a null callback.";
        var metadata = new Dictionary<string, object?>
        {
            ["behavior"] = behaviorType.FullName,
            ["stage"] = "behavior_guard",
            ["issue"] = "null_next"
        };
        failure = EncinaErrors.Create(EncinaErrorCodes.BehaviorNullNext, message, details: metadata);
        return false;
    }
}
