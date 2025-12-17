namespace SimpleMediator.AspNetCore;

/// <summary>
/// Provides access to the current <see cref="IRequestContext"/> for the current HTTP request.
/// </summary>
/// <remarks>
/// <para>
/// This accessor allows middleware and other ASP.NET Core components to set the request context
/// for the current HTTP request scope. The context is then available to all mediator handlers
/// processing requests within that HTTP request.
/// </para>
/// <para>
/// Implementation uses <see cref="System.Threading.AsyncLocal{T}"/> to ensure context flows
/// correctly across async operations within the same request scope.
/// </para>
/// </remarks>
public interface IRequestContextAccessor
{
    /// <summary>
    /// Gets or sets the current request context for this HTTP request.
    /// </summary>
    IRequestContext? RequestContext { get; set; }
}
