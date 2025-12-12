using System.Collections.Generic;
using System.Reflection;

namespace SimpleMediator;

/// <summary>
/// Guard clauses for notification pipeline validation.
/// </summary>
internal static class MediatorNotificationGuards
{
    public static bool TryValidateHandleMethod(MethodInfo method, Type handlerType, string notificationName, out MediatorError failure)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 2 || parameters[1].ParameterType != typeof(CancellationToken))
        {
            var exception = new TargetParameterCountException("The Handle method must accept the notification and a CancellationToken.");
            var metadata = new Dictionary<string, object?>
            {
                ["handler"] = handlerType.FullName,
                ["notification"] = notificationName,
                ["handleMethod"] = method.Name,
                ["parameterCount"] = parameters.Length
            };
            failure = MediatorErrors.FromException(MediatorErrorCodes.NotificationInvokeException, exception, $"Error invoking {handlerType.Name}.Handle.", metadata);
            return false;
        }

        if (!typeof(Task).IsAssignableFrom(method.ReturnType))
        {
            var message = $"Handler {handlerType.Name} returned an unexpected type while processing {notificationName}.";
            var metadata = new Dictionary<string, object?>
            {
                ["handler"] = handlerType.FullName,
                ["notification"] = notificationName,
                ["returnType"] = method.ReturnType.FullName
            };
            failure = MediatorErrors.Create(MediatorErrorCodes.NotificationInvalidReturn, message, details: metadata);
            return false;
        }

        failure = default;
        return true;
    }
}