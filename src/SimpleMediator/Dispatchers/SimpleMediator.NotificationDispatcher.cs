using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace SimpleMediator;

public sealed partial class SimpleMediator
{
    private static class NotificationDispatcher
    {
        public static async Task<Either<MediatorError, Unit>> ExecuteAsync<TNotification>(SimpleMediator mediator, TNotification notification, CancellationToken cancellationToken)
            where TNotification : INotification
        {
            using var scope = mediator._scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var notificationType = notification?.GetType() ?? typeof(TNotification);
            using var activity = MediatorDiagnostics.ActivitySource.HasListeners()
                ? MediatorDiagnostics.ActivitySource.StartActivity("SimpleMediator.Publish", ActivityKind.Internal)
                : null;
            activity?.SetTag("mediator.notification_type", notificationType.FullName);
            activity?.SetTag("mediator.notification_name", notificationType.Name);
            activity?.SetTag("mediator.notification_kind", "notification");

            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handlers = serviceProvider.GetServices(handlerType).ToList();
            activity?.SetTag("mediator.handler_count", handlers.Count);

            if (handlers.Count == 0)
            {
                Log.NoNotificationHandlers(mediator._logger, notificationType.Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Right<MediatorError, Unit>(Unit.Default);
            }

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                Log.SendingNotification(mediator._logger, notificationType.Name, handler.GetType().Name);
                var result = await InvokeNotificationHandler(handler, notification!, cancellationToken).ConfigureAwait(false);
                if (TryHandleNotificationFailure(mediator, notificationType.Name, activity, result, handler))
                {
                    return result;
                }
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Right<MediatorError, Unit>(Unit.Default);
        }

        private static bool TryHandleNotificationFailure(SimpleMediator mediator, string notificationName, Activity? activity, Either<MediatorError, Unit> result, object handlerInstance)
        {
            if (result.IsRight)
            {
                return false;
            }

            var error = result.Match(
                Left: err => err,
                Right: _ => MediatorErrors.Unknown);

            var errorCode = error.GetMediatorCode();
            activity?.SetStatus(ActivityStatusCode.Error, error.Message);
            activity?.SetTag("mediator.failure_reason", errorCode);
            var exception = error.Exception.Match(
                Some: ex => (Exception?)ex,
                None: () => null);
            var handlerTypeName = handlerInstance.GetType().Name;

            if (IsCancellationCode(errorCode))
            {
                Log.NotificationCancelled(mediator._logger, notificationName, handlerTypeName, exception);
            }
            else if (exception is not null)
            {
                Log.NotificationHandlerException(mediator._logger, notificationName, handlerTypeName, exception);
            }
            else
            {
                Log.NotificationHandlerFailure(mediator._logger, notificationName, handlerTypeName, error.Message);
            }

            return true;
        }

        internal static async Task<Either<MediatorError, Unit>> InvokeNotificationHandler<TNotification>(object handler, TNotification notification, CancellationToken cancellationToken)
            where TNotification : INotification
        {
            var handlerType = handler.GetType();
            var runtimeNotificationType = notification?.GetType();
            var desiredNotificationType = runtimeNotificationType
                ?? ResolveHandledNotificationType(handlerType)
                ?? typeof(TNotification);

            var notificationName = notification?.GetType().Name ?? typeof(TNotification).Name;

            if (!TryGetNotificationExecutor(handlerType, desiredNotificationType, runtimeNotificationType, notificationName, out var executor, out var failure))
            {
                return Left<MediatorError, Unit>(failure);
            }

            Task? execution;
            try
            {
                execution = executor(handler, notification, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    var message = $"Publishing {notificationName} was cancelled by {handlerType.Name}.";
                    var invokeMetadata = new Dictionary<string, object?>
                    {
                        ["handler"] = handlerType.FullName,
                        ["notification"] = notificationName,
                        ["stage"] = "invoke"
                    };
                    return Left<MediatorError, Unit>(MediatorErrors.Create(MediatorErrorCodes.NotificationCancelled, message, ex, invokeMetadata));
                }

                var invokeErrorMetadata = new Dictionary<string, object?>
                {
                    ["handler"] = handlerType.FullName,
                    ["notification"] = notificationName,
                    ["stage"] = "invoke"
                };
                var error = MediatorErrors.FromException(MediatorErrorCodes.NotificationException, ex, $"Error processing {notificationName} with {handlerType.Name}.", invokeErrorMetadata);
                return Left<MediatorError, Unit>(error);
            }
            catch (Exception ex)
            {
                var invokeExceptionMetadata = new Dictionary<string, object?>
                {
                    ["handler"] = handlerType.FullName,
                    ["notification"] = notificationName,
                    ["stage"] = "invoke"
                };
                var error = MediatorErrors.FromException(MediatorErrorCodes.NotificationException, ex, $"Error processing {notificationName} with {handlerType.Name}.", invokeExceptionMetadata);
                return Left<MediatorError, Unit>(error);
            }

            if (execution is null)
            {
                return Right<MediatorError, Unit>(Unit.Default);
            }

            try
            {
                await execution.ConfigureAwait(false);
                return Right<MediatorError, Unit>(Unit.Default);
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    var message = $"Publishing {notificationName} was cancelled by {handlerType.Name}.";
                    var executeMetadata = new Dictionary<string, object?>
                    {
                        ["handler"] = handlerType.FullName,
                        ["notification"] = notificationName,
                        ["stage"] = "execute"
                    };
                    return Left<MediatorError, Unit>(MediatorErrors.Create(MediatorErrorCodes.NotificationCancelled, message, ex, executeMetadata));
                }

                var executeErrorMetadata = new Dictionary<string, object?>
                {
                    ["handler"] = handlerType.FullName,
                    ["notification"] = notificationName,
                    ["stage"] = "execute"
                };
                var error = MediatorErrors.FromException(MediatorErrorCodes.NotificationException, ex, $"Error processing {notificationName} with {handlerType.Name}.", executeErrorMetadata);
                return Left<MediatorError, Unit>(error);
            }
            catch (Exception ex)
            {
                var invokeFailureMetadata = new Dictionary<string, object?>
                {
                    ["handler"] = handlerType.FullName,
                    ["notification"] = notificationName,
                    ["stage"] = "execute"
                };
                var error = MediatorErrors.FromException(MediatorErrorCodes.NotificationInvokeException, ex, $"Error invoking {handlerType.Name}.Handle.", invokeFailureMetadata);
                return Left<MediatorError, Unit>(error);
            }
        }

        private static bool TryGetNotificationExecutor(Type handlerType, Type desiredNotificationType, Type? runtimeNotificationType, string notificationName, out Func<object, object?, CancellationToken, Task> executor, out MediatorError failure)
        {
            if (NotificationHandlerInvokerCache.TryGetValue((handlerType, desiredNotificationType), out var cached))
            {
                executor = cached;
                failure = default;
                return true;
            }

            var method = ResolveHandleMethod(handlerType, desiredNotificationType, runtimeNotificationType);
            if (method is null)
            {
                var message = $"Handler {handlerType.Name} does not expose a compatible Handle method.";
                var metadata = new Dictionary<string, object?>
                {
                    ["handler"] = handlerType.FullName,
                    ["notification"] = notificationName,
                    ["expectedNotification"] = desiredNotificationType.FullName
                };
                failure = MediatorErrors.Create(MediatorErrorCodes.NotificationMissingHandle, message, details: metadata);
                executor = static (_, _, _) => Task.CompletedTask;
                return false;
            }

            if (!MediatorNotificationGuards.TryValidateHandleMethod(method, handlerType, notificationName, out failure))
            {
                executor = static (_, _, _) => Task.CompletedTask;
                return false;
            }

            var handledType = method.GetParameters()[0].ParameterType;
            executor = NotificationHandlerInvokerCache.GetOrAdd((handlerType, handledType), _ => CreateNotificationInvoker(method, handlerType, handledType));
            failure = default;
            return true;
        }

        private static Func<object, object?, CancellationToken, Task> CreateNotificationInvoker(MethodInfo method, Type handlerType, Type notificationType)
        {
            var handlerParameter = Expression.Parameter(typeof(object), "handler");
            var notificationParameter = Expression.Parameter(typeof(object), "notification");
            var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var castHandler = Expression.Convert(handlerParameter, handlerType);
            Expression castNotification;
            if (notificationType.IsValueType)
            {
                var tempVariable = Expression.Variable(notificationType, "typedNotification");
                var assignExpression = Expression.Block(
                    new[] { tempVariable },
                    Expression.IfThenElse(
                        Expression.Equal(notificationParameter, Expression.Constant(null, typeof(object))),
                        Expression.Assign(tempVariable, Expression.Default(notificationType)),
                        Expression.Assign(tempVariable, Expression.Convert(notificationParameter, notificationType))),
                    tempVariable);
                castNotification = assignExpression;
            }
            else
            {
                castNotification = Expression.Convert(notificationParameter, notificationType);
            }

            Expression call;
            if (method.IsStatic)
            {
                call = Expression.Call(method, castNotification, cancellationTokenParameter);
            }
            else
            {
                call = Expression.Call(castHandler, method, castNotification, cancellationTokenParameter);
            }

            Expression body = method.ReturnType == typeof(Task)
                ? call
                : Expression.Convert(call, typeof(Task));

            var lambda = Expression.Lambda<Func<object, object?, CancellationToken, Task>>(
                body,
                handlerParameter,
                notificationParameter,
                cancellationTokenParameter);

            return lambda.Compile();
        }

        private static Type? ResolveHandledNotificationType(Type handlerType)
        {
            var interfaceType = handlerType
                .GetInterfaces()
                .Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(@interface => @interface.GetGenericArguments()[0])
                .FirstOrDefault();

            return interfaceType;
        }

        private static MethodInfo? ResolveHandleMethod(Type handlerType, Type desiredNotificationType, Type? runtimeNotificationType)
        {
            var candidateMethods = handlerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                .Where(method => string.Equals(method.Name, "Handle", StringComparison.Ordinal))
                .ToArray();

            if (candidateMethods.Length == 0)
            {
                return null;
            }

            if (runtimeNotificationType is not null)
            {
                var runtimeMatch = candidateMethods.FirstOrDefault(method => HasCompatibleFirstParameter(method, runtimeNotificationType));
                if (runtimeMatch is not null)
                {
                    return runtimeMatch;
                }
            }

            var desiredMatch = candidateMethods.FirstOrDefault(method => HasCompatibleFirstParameter(method, desiredNotificationType));
            if (desiredMatch is not null)
            {
                return desiredMatch;
            }

            return candidateMethods.First();
        }

        private static bool HasCompatibleFirstParameter(MethodInfo method, Type candidateType)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return false;
            }

            var parameterType = parameters[0].ParameterType;
            return parameterType == candidateType
                || parameterType.IsAssignableFrom(candidateType)
                || candidateType.IsAssignableFrom(parameterType);
        }

        private static bool IsCancellationCode(string errorCode)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
            {
                return false;
            }

            return errorCode.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
        }
    }
}
