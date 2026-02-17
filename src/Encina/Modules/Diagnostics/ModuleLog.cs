using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Modules.Diagnostics;

/// <summary>
/// High-performance logging methods for modular monolith operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 1900-1999 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class ModuleLog
{
    /// <summary>
    /// Logs that a module is starting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="moduleName">The name of the module that is starting.</param>
    [LoggerMessage(
        EventId = 1900,
        Level = LogLevel.Information,
        Message = "Module {ModuleName} starting")]
    public static partial void ModuleStarting(
        ILogger logger,
        string moduleName);

    /// <summary>
    /// Logs that a module has started successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="moduleName">The name of the module that started.</param>
    [LoggerMessage(
        EventId = 1901,
        Level = LogLevel.Information,
        Message = "Module {ModuleName} started")]
    public static partial void ModuleStarted(
        ILogger logger,
        string moduleName);

    /// <summary>
    /// Logs that a module is stopping.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="moduleName">The name of the module that is stopping.</param>
    [LoggerMessage(
        EventId = 1902,
        Level = LogLevel.Information,
        Message = "Module {ModuleName} stopping")]
    public static partial void ModuleStopping(
        ILogger logger,
        string moduleName);

    /// <summary>
    /// Logs that a module has stopped successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="moduleName">The name of the module that stopped.</param>
    [LoggerMessage(
        EventId = 1903,
        Level = LogLevel.Information,
        Message = "Module {ModuleName} stopped")]
    public static partial void ModuleStopped(
        ILogger logger,
        string moduleName);

    /// <summary>
    /// Logs that a request is being dispatched to a module.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestType">The type name of the request being dispatched.</param>
    /// <param name="moduleName">The name of the target module.</param>
    [LoggerMessage(
        EventId = 1904,
        Level = LogLevel.Debug,
        Message = "Dispatching {RequestType} to module {ModuleName}")]
    public static partial void Dispatching(
        ILogger logger,
        string requestType,
        string moduleName);

    /// <summary>
    /// Logs that a dispatch to a module completed successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="moduleName">The name of the module that handled the dispatch.</param>
    [LoggerMessage(
        EventId = 1905,
        Level = LogLevel.Debug,
        Message = "Dispatch to module {ModuleName} completed")]
    public static partial void DispatchCompleted(
        ILogger logger,
        string moduleName);

    /// <summary>
    /// Logs that a module failed during a specific operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="moduleName">The name of the module that failed.</param>
    /// <param name="operation">The operation that was being performed when the failure occurred.</param>
    [LoggerMessage(
        EventId = 1906,
        Level = LogLevel.Error,
        Message = "Module {ModuleName} failed during {Operation}")]
    public static partial void ModuleFailed(
        ILogger logger,
        Exception exception,
        string moduleName,
        string operation);
}
