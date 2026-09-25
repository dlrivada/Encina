namespace CrapGateFixture;

// Synthetic fixture for crap-gate.cs's own report/enforce/exemption tests (issue #1346).
// Not part of the shipped Encina packages: it exists only so the gate has a deterministic,
// hand-computed CRAP scenario to run against, paired with SampleComplex.cobertura.xml and
// sample.diff in this same folder.
public static class SampleComplex
{
    // crap-exempt: single-question switch — routes a payload discriminator, single question
    public static string Classify(int kind)
    {
        switch (kind)
        {
            case 1: return "a";
            case 2: return "b";
            case 3: return "c";
            case 4: return "d";
            case 5: return "e";
            default: return "z";
        }
    }

    public static int RiskyMethod(int a, int b, int c, int d)
    {
        if (a > 0)
        {
            if (b > 0)
            {
                if (c > 0)
                {
                    if (d > 0)
                    {
                        return a + b + c + d;
                    }
                    return a + b + c;
                }
                return a + b;
            }
            return a;
        }
        return 0;
    }
}
