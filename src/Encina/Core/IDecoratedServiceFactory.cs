using Microsoft.Extensions.DependencyInjection;

namespace Encina;

/// <summary>
/// Implemented by a service factory that wraps another registration (a decorator), so registration
/// code can look through the wrapper and see what it decorates.
/// </summary>
/// <remarks>
/// A decorating registration is a factory descriptor whose <see cref="ServiceDescriptor.ImplementationFactory"/>
/// is a method of an object implementing this interface; the descriptor itself no longer names the
/// decorated implementation type.
/// </remarks>
public interface IDecoratedServiceFactory
{
    /// <summary>
    /// Gets the registration this factory decorates, as it was before it was wrapped.
    /// </summary>
    ServiceDescriptor Decorated { get; }
}
