namespace Encina.Modules.Isolation;

/// <summary>
/// Default implementation of <see cref="IModuleExecutionContext"/> using <see cref="AsyncLocal{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="AsyncLocal{T}"/> to provide ambient module context
/// that automatically flows across async/await boundaries. Each async execution context
/// has its own isolated module value.
/// </para>
/// <para>
/// The implementation is thread-safe and supports nested scopes, though nesting
/// is generally not recommended as it may indicate a design issue.
/// </para>
/// </remarks>
public sealed class ModuleExecutionContext : IModuleExecutionContext
{
    private static readonly AsyncLocal<string?> _currentModule = new();

    /// <inheritdoc />
    public string? CurrentModule => _currentModule.Value;

    /// <inheritdoc />
    public void SetModule(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        _currentModule.Value = moduleName;
    }

    /// <inheritdoc />
    public void ClearModule()
    {
        _currentModule.Value = null;
    }

    /// <inheritdoc />
    public IDisposable CreateScope(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        return new ModuleExecutionScope(this, moduleName);
    }

    /// <summary>
    /// A disposable scope that sets the module on creation and clears it on disposal.
    /// </summary>
    private sealed class ModuleExecutionScope : IDisposable
    {
        private readonly ModuleExecutionContext _context;
        private readonly string? _previousModule;
        private bool _disposed;

        public ModuleExecutionScope(ModuleExecutionContext context, string moduleName)
        {
            _context = context;
            _previousModule = context.CurrentModule;
            context.SetModule(moduleName);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Restore previous module or clear if there was none
            if (_previousModule is not null)
            {
                _context.SetModule(_previousModule);
            }
            else
            {
                _context.ClearModule();
            }
        }
    }
}
