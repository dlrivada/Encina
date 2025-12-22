using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina;

public sealed partial class Encina
{
    /// <summary>
    /// Internal dispatcher for publishing notifications to all registered handlers in parallel.
    /// </summary>
    /// <remarks>
    /// <para><b>Key Differences from RequestDispatcher:</b></para>
    /// <list type="bullet">
    /// <item>Broadcasts to MULTIPLE handlers instead of routing to a single handler</item>
    /// <item>Handlers are resolved dynamically using IEnumerable&lt;INotificationHandler&lt;T&gt;&gt;</item>
    /// <item>Supports polymorphic resolution (handler for base type receives derived notifications)</item>
    /// <item>First handler failure stops iteration and returns error (fail-fast)</item>
    /// <item>No behaviors or processors - notifications are simpler than requests</item>
    /// </list>
    /// <para><b>Resolution Strategy:</b></para>
    /// <para>Uses reflection-generated compiled delegates (Expression trees) to invoke handlers efficiently.
    /// Delegates are cached per (handler type, notification type) pair to avoid repeated compilation.</para>
    /// <para><b>Polymorphism Support:</b></para>
    /// <para>If notification runtime type differs from compile-time type, attempts to resolve handlers for
    /// both types, preferring the more specific (runtime) type. This enables interface-based notifications.</para>
    /// <para><b>Error Handling:</b></para>
    /// <para>Returns Either&lt;EncinaError, Unit&gt;. If ANY handler fails, the entire operation returns Left
    /// with the first error encountered. This is deliberate - notifications should be reliable or fail fast.</para>
    /// </remarks>
    private static class NotificationDispatcher
    {
        public static async Task<Either<EncinaError, Unit>> ExecuteAsync<TNotification>(Encina Encina, TNotification notification, CancellationToken cancellationToken)
            where TNotification : INotification
        {
            // --- SETUP PHASE ---
            // Create scope for handler resolution
            using IServiceScope scope = Encina._scopeFactory.CreateScope();
            IServiceProvider serviceProvider = scope.ServiceProvider;

            // Determine actual notification type (may differ from TNotification due to polymorphism)
            Type notificationType = notification?.GetType() ?? typeof(TNotification);
            using Activity? activity = EncinaDiagnostics.ActivitySource.HasListeners()
                ? EncinaDiagnostics.ActivitySource.StartActivity("Encina.Publish", ActivityKind.Internal)
                : null;
            activity?.SetTag("Encina.notification_type", notificationType.FullName);
            activity?.SetTag("Encina.notification_name", notificationType.Name);
            activity?.SetTag("Encina.notification_kind", "notification");

            // --- HANDLER RESOLUTION PHASE ---
            // Resolve ALL handlers registered for this notification type
            // Multiple handlers can be registered for the same notification (observer pattern)
            Type handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handlersList = serviceProvider.GetServices(handlerType).Where(h => h is not null).Cast<object>().ToList();
            activity?.SetTag("Encina.handler_count", handlersList.Count);

            // Zero handlers is not an error - notifications are fire-and-forget
            if (handlersList.Count == 0)
            {
                Log.NoNotificationHandlers(Encina._logger, notificationType.Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Right<EncinaError, Unit>(Unit.Default);
            }

            // --- EXECUTION PHASE ---
            // Use the configured dispatch strategy (Sequential, Parallel, or ParallelWhenAll)
            activity?.SetTag("Encina.dispatch_strategy", Encina._notificationOptions.Strategy.ToString());

            Either<EncinaError, Unit> result = await Encina._dispatchStrategy.DispatchAsync(
                handlersList,
                notification!,
                async (handler, notif, ct) =>
                {
                    Log.SendingNotification(Encina._logger, notificationType.Name, handler.GetType().Name);
                    Either<EncinaError, Unit> handlerResult = await InvokeNotificationHandler(handler, notif, ct).ConfigureAwait(false);

                    // Log failures
                    if (handlerResult.IsLeft)
                    {
                        TryHandleNotificationFailure(Encina, notificationType.Name, activity, handlerResult, handler);
                    }

                    return handlerResult;
                },
                cancellationToken).ConfigureAwait(false);

            if (result.IsLeft)
            {
                return result;
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Right<EncinaError, Unit>(Unit.Default);
        }

        private static bool TryHandleNotificationFailure(Encina Encina, string notificationName, Activity? activity, Either<EncinaError, Unit> result, object handlerInstance)
        {
            if (result.IsRight)
            {
                return false;
            }

            EncinaError error = result.Match(
                Left: err => err,
                Right: _ => EncinaErrors.Unknown);

            string errorCode = error.GetEncinaCode();
            activity?.SetStatus(ActivityStatusCode.Error, error.Message);
            activity?.SetTag("Encina.failure_reason", errorCode);
            Exception? exception = error.Exception.Match(
                Some: ex => (Exception?)ex,
                None: () => null);
            string handlerTypeName = handlerInstance.GetType().Name;

            if (IsCancellationCode(errorCode))
            {
                Log.NotificationCancelled(Encina._logger, notificationName, handlerTypeName, exception);
            }
            else if (exception is not null)
            {
                Log.NotificationHandlerException(Encina._logger, notificationName, handlerTypeName, exception);
            }
            else
            {
                Log.NotificationHandlerFailure(Encina._logger, notificationName, handlerTypeName, error.Message);
            }

            return true;
        }

        /// <summary>
        /// Invokes a notification handler that now returns Either{EncinaError, Unit}.
        /// </summary>
        /// <remarks>
        /// Since handlers now return Either, they handle their own functional failures.
        /// We only catch unexpected exceptions as a safety net for bugs.
        /// </remarks>
        internal static async Task<Either<EncinaError, Unit>> InvokeNotificationHandler<TNotification>(object handler, TNotification notification, CancellationToken cancellationToken)
            where TNotification : INotification
        {
            Type handlerType = handler.GetType();
            Type? runtimeNotificationType = notification?.GetType();
            Type desiredNotificationType = runtimeNotificationType
                ?? ResolveHandledNotificationType(handlerType)
                ?? typeof(TNotification);

            string notificationName = notification?.GetType().Name ?? typeof(TNotification).Name;

            if (!TryGetNotificationExecutor(handlerType, desiredNotificationType, runtimeNotificationType, notificationName, out Func<object, object?, CancellationToken, Task<Either<EncinaError, Unit>>>? executor, out EncinaError failure))
            {
                return Left<EncinaError, Unit>(failure);
            }

            try
            {
                // Executor now returns Task<Either<EncinaError, Unit>>
                Either<EncinaError, Unit> result = await executor(handler, notification, cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                // Cancellation is expected behavior
                string message = $"Notification handler {handlerType.Name} was cancelled while processing {notificationName}.";
                var metadata = new Dictionary<string, object?>
                {
                    ["handler"] = handlerType.FullName,
                    ["notification"] = notificationName,
                    ["stage"] = "handler"
                };
                return Left<EncinaError, Unit>(EncinaErrors.Create(EncinaErrorCodes.NotificationCancelled, message, ex, metadata));
            }
            catch (Exception ex)
            {
                // Unexpected exception - indicates a bug in the handler
                // Handlers should return Left for expected failures instead of throwing
                string message = $"Unexpected exception in notification handler {handlerType.Name} for {notificationName}. " +
                              $"Handlers should return Left for expected failures instead of throwing exceptions.";
                var metadata = new Dictionary<string, object?>
                {
                    ["handler"] = handlerType.FullName,
                    ["notification"] = notificationName,
                    ["stage"] = "handler",
                    ["exception_type"] = ex.GetType().FullName
                };
                EncinaError error = EncinaErrors.FromException(EncinaErrorCodes.NotificationException, ex, message, metadata);
                return Left<EncinaError, Unit>(error);
            }
        }

        private static bool TryGetNotificationExecutor(Type handlerType, Type desiredNotificationType, Type? runtimeNotificationType, string notificationName, out Func<object, object?, CancellationToken, Task<Either<EncinaError, Unit>>> executor, out EncinaError failure)
        {
            if (NotificationHandlerInvokerCache.TryGetValue((handlerType, desiredNotificationType), out Func<object, object?, CancellationToken, Task<Either<EncinaError, Unit>>>? cached))
            {
                executor = cached;
                failure = default;
                return true;
            }

            MethodInfo? method = ResolveHandleMethod(handlerType, desiredNotificationType, runtimeNotificationType);
            if (method is null)
            {
                string message = $"Handler {handlerType.Name} does not expose a compatible Handle method.";
                var metadata = new Dictionary<string, object?>
                {
                    ["handler"] = handlerType.FullName,
                    ["notification"] = notificationName,
                    ["expectedNotification"] = desiredNotificationType.FullName
                };
                failure = EncinaErrors.Create(EncinaErrorCodes.NotificationMissingHandle, message, details: metadata);
                executor = static (_, _, _) => Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
                return false;
            }

            if (!EncinaNotificationGuards.TryValidateHandleMethod(method, handlerType, notificationName, out failure))
            {
                executor = static (_, _, _) => Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
                return false;
            }

            Type handledType = method.GetParameters()[0].ParameterType;
            executor = NotificationHandlerInvokerCache.GetOrAdd((handlerType, handledType), _ => CreateNotificationInvoker(method, handlerType, handledType));
            failure = default;
            return true;
        }

        /// <summary>
        /// Generates a compiled delegate that can invoke the handler's Handle method without reflection.
        /// </summary>
        /// <param name="method">The MethodInfo for the Handle method to invoke.</param>
        /// <param name="handlerType">The concrete type of the handler.</param>
        /// <param name="notificationType">The notification type the handler expects.</param>
        /// <returns>A compiled delegate that performs type casting and method invocation.</returns>
        /// <remarks>
        /// <para><b>Performance Optimization:</b></para>
        /// <para>This method uses Expression trees to generate optimized IL at runtime, which is then JIT-compiled.
        /// The resulting delegate is nearly as fast as direct method invocation, avoiding the overhead of
        /// MethodInfo.Invoke (reflection) on every notification publication.</para>
        /// <para><b>Value Type Handling:</b></para>
        /// <para>Special care is taken for value type notifications to avoid boxing/unboxing where possible.
        /// If the notification is null and the type is a value type, we use the default value instead of
        /// attempting to cast null.</para>
        /// <para><b>Generated Delegate Signature:</b></para>
        /// <para>Func&lt;object handler, object? notification, CancellationToken, Task&lt;Either&lt;EncinaError, Unit&gt;&gt;&gt;</para>
        /// <para>This allows the cached delegate to be invoked with object parameters while maintaining type safety
        /// internally through the generated casts. The handler now returns Either to enable Railway Oriented Programming.</para>
        /// </remarks>
        private static Func<object, object?, CancellationToken, Task<Either<EncinaError, Unit>>> CreateNotificationInvoker(MethodInfo method, Type handlerType, Type notificationType)
        {
            // Define parameters for the lambda: (object handler, object? notification, CancellationToken cancellationToken)
            ParameterExpression handlerParameter = Expression.Parameter(typeof(object), "handler");
            ParameterExpression notificationParameter = Expression.Parameter(typeof(object), "notification");
            ParameterExpression cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            // Cast handler from object to its concrete type
            UnaryExpression castHandler = Expression.Convert(handlerParameter, handlerType);

            // Cast notification - special handling for value types to avoid boxing issues
            Expression castNotification;
            if (notificationType.IsValueType)
            {
                // For value types: if notification is null, use default(TNotification) instead of casting null
                ParameterExpression tempVariable = Expression.Variable(notificationType, "typedNotification");
                BlockExpression assignExpression = Expression.Block(
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
                // For reference types: simple cast is sufficient
                castNotification = Expression.Convert(notificationParameter, notificationType);
            }

            // Generate the method call expression
            Expression call;
            if (method.IsStatic)
            {
                // Static method: Handle(notification, cancellationToken)
                call = Expression.Call(method, castNotification, cancellationTokenParameter);
            }
            else
            {
                // Instance method: handler.Handle(notification, cancellationToken)
                call = Expression.Call(castHandler, method, castNotification, cancellationTokenParameter);
            }

            // Handler now returns Task<Either<EncinaError, Unit>>
            // No conversion needed - it's already the correct type
            Expression body = call;

            // Build and compile the lambda expression
            var lambda = Expression.Lambda<Func<object, object?, CancellationToken, Task<Either<EncinaError, Unit>>>>(
                body,
                handlerParameter,
                notificationParameter,
                cancellationTokenParameter);

            return lambda.Compile();
        }

        private static Type? ResolveHandledNotificationType(Type handlerType)
        {
            Type? interfaceType = handlerType
                .GetInterfaces()
                .Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(@interface => @interface.GetGenericArguments()[0])
                .FirstOrDefault();

            return interfaceType;
        }

        private static MethodInfo? ResolveHandleMethod(Type handlerType, Type desiredNotificationType, Type? runtimeNotificationType)
        {
            MethodInfo[] candidateMethods = handlerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                .Where(method => string.Equals(method.Name, "Handle", StringComparison.Ordinal))
                .ToArray();

            if (candidateMethods.Length == 0)
            {
                return null;
            }

            if (runtimeNotificationType is not null)
            {
                MethodInfo? runtimeMatch = candidateMethods.FirstOrDefault(method => HasCompatibleFirstParameter(method, runtimeNotificationType));
                if (runtimeMatch is not null)
                {
                    return runtimeMatch;
                }
            }

            MethodInfo? desiredMatch = candidateMethods.FirstOrDefault(method => HasCompatibleFirstParameter(method, desiredNotificationType));
            if (desiredMatch is not null)
            {
                return desiredMatch;
            }

            return candidateMethods.First();
        }

        private static bool HasCompatibleFirstParameter(MethodInfo method, Type candidateType)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return false;
            }

            Type parameterType = parameters[0].ParameterType;
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
