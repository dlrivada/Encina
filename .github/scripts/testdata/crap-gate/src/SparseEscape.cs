namespace CrapGateFixture;

// Synthetic fixture for crap-gate.cs's fallback-safety test (issue #1506, PR #1508 review). A
// large, complex, poorly-covered "Enclosing" method whose Cobertura <lines> are SPARSE (only a
// couple of lines recorded, not every line in its textual span) contains a small, fully-covered
// nested "Helper" closure. The changed line sits inside Helper's numeric range AND Enclosing's
// numeric range, but is recorded as an exact <line> by neither method (e.g. a comment with no
// sequence point) — so the gate must fall back to the range check, and the fallback must still
// attribute the line to Enclosing (the real, large violation), not only to the small Helper.
// Paired with SparseEscape.cobertura.xml and sparse-escape.diff in this same folder.
public static class SparseEscape
{
    public static int Enclosing(int input)
    {
        var before = input + 1;

        var result = Helper(before, x =>
        {
            var doubled = x * 2;
            // old comment inside Helper's body, no sequence point
            return doubled;
        });

        var after = result + 1;
        return after;
    }

    private static int Helper(int value, Func<int, int> transform) => transform(value);
}
