namespace Encina.Modules.Isolation;

/// <summary>
/// Provides ambient access to the currently executing module context.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables tracking which module is currently executing a request,
/// which is essential for database isolation validation. When a handler executes,
/// the pipeline behavior sets the current module, allowing interceptors and
/// validators to know which schemas should be accessible.
/// </para>
/// <para>
/// The context flows automatically through async operations, so child tasks
/// inherit the module context from their parent.
/// </para>
/// <para>
/// This is typically used by:
/// <list type="bullet">
/// <item><description>Pipeline behaviors that set the module context before handler execution</description></item>
/// <item><description>Database interceptors that validate SQL against allowed schemas</description></item>
/// <item><description>Connection factories that select the appropriate connection string</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a pipeline behavior
/// public async Task&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(...)
/// {
///     var moduleName = _handlerRegistry.GetModuleName(typeof(THandler));
///     if (moduleName is not null)
///     {
///         _executionContext.SetModule(moduleName);
///         try
///         {
///             return await next();
///         }
///         finally
///         {
///             _executionContext.ClearModule();
///         }
///     }
///     return await next();
/// }
///
/// // In a database interceptor
/// public void OnCommandExecuting(...)
/// {
///     var currentModule = _executionContext.CurrentModule;
///     if (currentModule is not null)
///     {
///         ValidateSchemaAccess(command.CommandText, currentModule);
///     }
/// }
/// </code>
/// </example>
public interface IModuleExecutionContext
{
    /// <summary>
    /// Gets the name of the currently executing module.
    /// </summary>
    /// <value>
    /// The module name if a module is currently executing; otherwise, <c>null</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property returns <c>null</c> when:
    /// <list type="bullet">
    /// <item><description>No handler is currently executing</description></item>
    /// <item><description>The handler is not associated with any module</description></item>
    /// <item><description>The context was explicitly cleared</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The value is thread-local and async-aware, so each execution context
    /// has its own isolated module value.
    /// </para>
    /// </remarks>
    string? CurrentModule { get; }

    /// <summary>
    /// Sets the currently executing module.
    /// </summary>
    /// <param name="moduleName">The name of the module to set as current.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="moduleName"/> is <c>null</c> or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This should be called at the start of request handling, typically by
    /// a pipeline behavior that determines the module from the handler type.
    /// </para>
    /// <para>
    /// The module context remains set until <see cref="ClearModule"/> is called
    /// or the async context is disposed.
    /// </para>
    /// <para>
    /// Calling this method when a module is already set will overwrite the
    /// previous value. Use <see cref="ClearModule"/> to restore the previous state.
    /// </para>
    /// </remarks>
    void SetModule(string moduleName);

    /// <summary>
    /// Clears the currently executing module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be called after request handling completes, typically in a
    /// finally block to ensure cleanup even if an exception occurs.
    /// </para>
    /// <para>
    /// After calling this method, <see cref="CurrentModule"/> returns <c>null</c>
    /// until <see cref="SetModule"/> is called again.
    /// </para>
    /// </remarks>
    void ClearModule();

    /// <summary>
    /// Creates a scope that automatically clears the module when disposed.
    /// </summary>
    /// <param name="moduleName">The name of the module to set as current.</param>
    /// <returns>
    /// A disposable scope that clears the module context when disposed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that combines <see cref="SetModule"/> and
    /// <see cref="ClearModule"/> into a using pattern:
    /// </para>
    /// <code>
    /// using (_executionContext.CreateScope("Orders"))
    /// {
    ///     // CurrentModule is "Orders" here
    ///     await next();
    /// }
    /// // CurrentModule is null (or previous value) here
    /// </code>
    /// <para>
    /// The scope is automatically disposed even if an exception occurs,
    /// ensuring the module context is always properly cleaned up.
    /// </para>
    /// </remarks>
    IDisposable CreateScope(string moduleName);
}
