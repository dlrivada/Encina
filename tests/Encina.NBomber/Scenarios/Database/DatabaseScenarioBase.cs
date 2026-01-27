using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Base class for database load testing scenarios.
/// Provides common setup, teardown, and NBomber Response helper methods.
/// </summary>
public abstract class DatabaseScenarioBase : IAsyncDisposable
{
    /// <summary>
    /// Gets the scenario context containing provider factory and shared state.
    /// </summary>
    protected DatabaseScenarioContext Context { get; }

    /// <summary>
    /// Gets the scenario name.
    /// </summary>
    public abstract string ScenarioName { get; }

    /// <summary>
    /// Gets the scenario description.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseScenarioBase"/> class.
    /// </summary>
    /// <param name="context">The database scenario context.</param>
    protected DatabaseScenarioBase(DatabaseScenarioContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a successful NBomber response with optional payload size.
    /// </summary>
    /// <param name="sizeBytes">Optional payload size in bytes for metrics.</param>
    /// <returns>A successful NBomber response.</returns>
    protected static IResponse CreateOkResponse(int sizeBytes = 0)
    {
        return sizeBytes > 0
            ? Response.Ok(sizeBytes: sizeBytes)
            : Response.Ok();
    }

    /// <summary>
    /// Creates a failed NBomber response with error details.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="statusCode">Optional status code for categorization.</param>
    /// <returns>A failed NBomber response.</returns>
    protected static IResponse CreateFailResponse(string errorMessage, string? statusCode = null)
    {
        return Response.Fail(errorMessage, statusCode: statusCode ?? "db_error");
    }

    /// <summary>
    /// Creates a failed NBomber response from an exception.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A failed NBomber response.</returns>
    protected static IResponse CreateFailResponse(Exception exception)
    {
        var statusCode = exception switch
        {
            TimeoutException => "timeout",
            InvalidOperationException => "invalid_operation",
            _ => "db_error"
        };

        return Response.Fail(exception.Message, statusCode: statusCode);
    }

    /// <summary>
    /// Executes a database operation and returns an NBomber response.
    /// Wraps exceptions into failed responses.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>An NBomber response.</returns>
    protected static async Task<IResponse> ExecuteAsync(Func<Task> operation)
    {
        try
        {
            await operation().ConfigureAwait(false);
            return CreateOkResponse();
        }
        catch (Exception ex)
        {
            return CreateFailResponse(ex);
        }
    }

    /// <summary>
    /// Executes a database operation that returns a value and creates an NBomber response.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="validator">Optional validator for the result.</param>
    /// <returns>An NBomber response.</returns>
    protected static async Task<IResponse> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Func<T, bool>? validator = null)
    {
        try
        {
            var result = await operation().ConfigureAwait(false);

            if (validator is not null && !validator(result))
            {
                return CreateFailResponse("Validation failed", "validation_error");
            }

            return CreateOkResponse();
        }
        catch (Exception ex)
        {
            return CreateFailResponse(ex);
        }
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
