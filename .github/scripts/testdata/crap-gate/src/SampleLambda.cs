namespace CrapGateFixture;

// Synthetic fixture for crap-gate.cs's nested-lambda attribution test (issue #1506). Mirrors the
// shape found in #1503: an enclosing method whose body contains a lambda passed to a helper. The
// compiler emits the lambda as its own method, whose small set of own lines sits numerically
// inside the enclosing method's MinLine..MaxLine range even though the enclosing method's own
// recorded Cobertura lines never include them. Paired with SampleLambda.cobertura.xml and
// sample-lambda.diff in this same folder.
public static class SampleLambda
{
    public static int Enclosing(int input)
    {
        var before = input + 1;

        var result = Apply(before, x =>
        {
            var doubled = x * 2;
            return doubled;
        });

        var after = result + 1;
        return after;
    }

    private static int Apply(int value, Func<int, int> transform) => transform(value);
}
