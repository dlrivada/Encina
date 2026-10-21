// Fixture for release-readiness.cs --self-test: never compiled.
// One Skip= references an issue on the line above, one on the same line, one references none.
public class SkipSample
{
    // Waits for #123 to land.
    [Fact(Skip = "pending the fix")]
    public void Referenced_on_the_line_above() { }

    [Fact(Skip = "pending #456")]
    public void Referenced_on_the_same_line() { }

    [Fact(Skip = "no reason given")]
    public void Not_referenced() { }
}
