using System.Collections.Generic;
using System.Reflection;
using LanguageExt;

namespace Encina;

/// <summary>
/// Guard clauses for notification pipeline validation.
/// </summary>
internal static class EncinaNotificationGuards
{
    public static bool TryValidateHandleMethod(MethodInfo method, Type handlerType, string notificationName, out EncinaError failure)
    {
        ParameterInfo[] parameters = method.GetParameters();
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
            failure = EncinaErrors.FromException(EncinaErrorCodes.NotificationInvokeException, exception, $"Error invoking {handlerType.Name}.Handle.", metadata);
            return false;
        }

        // Validate return type is Task<Either<EncinaError, Unit>>
        Type expectedReturnType = typeof(Task<>).MakeGenericType(typeof(Either<EncinaError, Unit>));
        if (method.ReturnType != expectedReturnType)
        {
            string message = $"Handler {handlerType.Name} must return Task<Either<EncinaError, Unit>> but returned {method.ReturnType.Name}.";
            var metadata = new Dictionary<string, object?>
            {
                ["handler"] = handlerType.FullName,
                ["notification"] = notificationName,
                ["expectedReturnType"] = expectedReturnType.FullName,
                ["actualReturnType"] = method.ReturnType.FullName
            };
            failure = EncinaErrors.Create(EncinaErrorCodes.NotificationInvalidReturn, message, details: metadata);
            return false;
        }

        failure = default;
        return true;
    }
}