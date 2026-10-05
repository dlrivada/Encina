```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   420.20 ns |     7.855 ns |   4.674 ns |  4.29 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   555.47 ns |     1.769 ns |   1.053 ns |  5.67 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 2,472.73 ns |     8.976 ns |   5.342 ns | 25.24 |    0.15 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,932.88 ns |    12.740 ns |   8.427 ns | 40.15 |    0.24 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 4,027.70 ns |    23.039 ns |  13.710 ns | 41.12 |    0.26 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   518.39 ns |     4.249 ns |   2.528 ns |  5.29 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    97.95 ns |     0.869 ns |   0.575 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 9,646.94 ns |    73.616 ns |  48.692 ns | 98.49 |    0.72 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 4,047.07 ns |    13.033 ns |   8.621 ns | 41.32 |    0.24 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   433.31 ns |    10.258 ns |   6.785 ns |  4.42 |    0.07 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |            |       |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   419.94 ns |    64.467 ns |   3.534 ns |  4.32 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   539.22 ns |    26.631 ns |   1.460 ns |  5.54 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 2,493.97 ns | 1,131.857 ns |  62.041 ns | 25.63 |    0.56 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 4,080.03 ns |   159.105 ns |   8.721 ns | 41.93 |    0.20 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,907.07 ns |   414.308 ns |  22.710 ns | 40.15 |    0.27 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   521.41 ns |    69.704 ns |   3.821 ns |  5.36 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    97.31 ns |     9.185 ns |   0.503 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 9,462.35 ns | 3,056.691 ns | 167.548 ns | 97.24 |    1.55 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 4,072.49 ns | 1,788.362 ns |  98.026 ns | 41.85 |    0.89 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   431.82 ns |    85.662 ns |   4.695 ns |  4.44 |    0.05 | 0.0310 |     520 B |        2.32 |
