using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying EitherExtensions public API contract.
/// </summary>
public sealed class EitherExtensionsContracts
{
    private readonly Type _extensionsType = typeof(EitherExtensions);

    #region Class Structure Contracts

    [Fact]
    public void EitherExtensions_MustBeStaticClass()
    {
        _extensionsType.IsAbstract.Should().BeTrue();
        _extensionsType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void EitherExtensions_MustBePublic()
    {
        _extensionsType.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void EitherExtensions_AllMethodsMustBeExtensionMethods()
    {
        // Extension methods are marked with ExtensionAttribute on the method itself
        var extensionMethods = _extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName && m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
            .ToList();

        extensionMethods.Should().NotBeEmpty("EitherExtensions should have extension methods");

        // Verify all public static methods are extension methods
        var allPublicStaticMethods = _extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName)
            .ToList();

        extensionMethods.Should().HaveCount(allPublicStaticMethods.Count, "all public static methods should be extension methods");
    }

    #endregion

    #region Combine Methods Contracts

    [Fact]
    public void EitherExtensions_MustHaveCombine2Method()
    {
        // Combine with 2 Either values returns tuple
        var method = _extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "Combine" && m.GetParameters().Length == 2);
        method.Should().NotBeNull("Combine(e1, e2) should exist");
    }

    [Fact]
    public void EitherExtensions_MustHaveCombine3Method()
    {
        // Combine with 3 Either values returns tuple
        var method = _extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "Combine" && m.GetParameters().Length == 3);
        method.Should().NotBeNull("Combine(e1, e2, e3) should exist");
    }

    [Fact]
    public void EitherExtensions_MustHaveCombine4Method()
    {
        // Combine with 4 Either values returns tuple
        var method = _extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "Combine" && m.GetParameters().Length == 4);
        method.Should().NotBeNull("Combine(e1, e2, e3, e4) should exist");
    }

    [Fact]
    public void EitherExtensions_MustHaveCombineCollectionMethod()
    {
        // Combine on IEnumerable<Either<TError, T>> returns Either<TError, IReadOnlyList<T>>
        var method = _extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "Combine" && m.GetParameters().Length == 1);
        method.Should().NotBeNull("Combine(IEnumerable<Either>) should exist");
    }

    #endregion

    #region Conditional Methods Contracts

    [Fact]
    public void EitherExtensions_MustHaveWhenMethod()
    {
        var method = _extensionsType.GetMethod("When");
        method.Should().NotBeNull();
    }

    [Fact]
    public void EitherExtensions_MustHaveEnsureMethod()
    {
        var method = _extensionsType.GetMethod("Ensure");
        method.Should().NotBeNull();
    }

    [Fact]
    public void EitherExtensions_MustHaveOrElseMethod()
    {
        var method = _extensionsType.GetMethod("OrElse");
        method.Should().NotBeNull();
    }

    [Fact]
    public void EitherExtensions_MustHaveGetOrDefaultMethod()
    {
        var method = _extensionsType.GetMethod("GetOrDefault");
        method.Should().NotBeNull();
    }

    [Fact]
    public void EitherExtensions_MustHaveGetOrElseMethod()
    {
        var method = _extensionsType.GetMethod("GetOrElse");
        method.Should().NotBeNull();
    }

    #endregion

    #region Side Effect Methods Contracts

    [Fact]
    public void EitherExtensions_MustHaveTapMethod()
    {
        var method = _extensionsType.GetMethod("Tap");
        method.Should().NotBeNull();
    }

    [Fact]
    public void EitherExtensions_MustHaveTapErrorMethod()
    {
        var method = _extensionsType.GetMethod("TapError");
        method.Should().NotBeNull();
    }

    #endregion

    #region Async Methods Contracts

    [Fact]
    public void EitherExtensions_MustHaveBindAsyncMethods()
    {
        // BindAsync has multiple overloads (Task<Either> and Either extensions)
        var methods = _extensionsType.GetMethods()
            .Where(m => m.Name == "BindAsync")
            .ToList();
        methods.Should().HaveCountGreaterThanOrEqualTo(2, "BindAsync should have overloads for Task<Either> and Either");
    }

    [Fact]
    public void EitherExtensions_MustHaveMapAsyncMethods()
    {
        // MapAsync has multiple overloads
        var methods = _extensionsType.GetMethods()
            .Where(m => m.Name == "MapAsync")
            .ToList();
        methods.Should().HaveCountGreaterThanOrEqualTo(2, "MapAsync should have overloads for Task<Either> and Either");
    }

    [Fact]
    public void EitherExtensions_MustHaveTapAsyncMethod()
    {
        var methods = _extensionsType.GetMethods()
            .Where(m => m.Name == "TapAsync")
            .ToList();
        methods.Should().NotBeEmpty("TapAsync should exist");
    }

    #endregion

    #region Conversion Methods Contracts

    [Fact]
    public void EitherExtensions_MustHaveToOptionMethod()
    {
        var method = _extensionsType.GetMethod("ToOption");
        method.Should().NotBeNull();
    }

    [Fact]
    public void EitherExtensions_MustHaveToEitherMethod()
    {
        var method = _extensionsType.GetMethod("ToEither");
        method.Should().NotBeNull();
    }

    [Fact]
    public void EitherExtensions_MustHaveGetOrThrowMethod()
    {
        var method = _extensionsType.GetMethod("GetOrThrow");
        method.Should().NotBeNull();
    }

    #endregion
}
