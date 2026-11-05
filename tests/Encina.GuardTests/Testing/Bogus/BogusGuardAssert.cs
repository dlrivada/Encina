namespace Encina.GuardTests.Testing.Bogus;

/// <summary>
/// Asserts the exact exception type and parameter name of an argument check.
/// </summary>
internal static class BogusGuardAssert
{
    public static void Throws<TException>(Action action, string expectedParamName)
        where TException : ArgumentException
    {
        var exception = Should.Throw<TException>(action);
        exception.GetType().ShouldBe(typeof(TException));
        exception.ParamName.ShouldBe(expectedParamName);
    }

    /// <summary>
    /// A null string makes <c>ArgumentException.ThrowIfNullOrWhiteSpace</c> throw
    /// <see cref="ArgumentNullException"/>; an empty or whitespace string makes it throw
    /// <see cref="ArgumentException"/>.
    /// </summary>
    public static void RejectsNullEmptyAndWhitespace(Action<string> call, string expectedParamName)
    {
        Throws<ArgumentNullException>(() => call(null!), expectedParamName);
        Throws<ArgumentException>(() => call(string.Empty), expectedParamName);
        Throws<ArgumentException>(() => call("   "), expectedParamName);
    }
}
