```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   372.91 ns |   1.360 ns |  0.900 ns |  3.86 |    0.06 | 0.0062 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   474.72 ns |   1.820 ns |  1.204 ns |  4.91 |    0.07 | 0.0048 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,941.53 ns |  27.066 ns | 14.156 ns | 20.09 |    0.33 | 0.0114 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,215.13 ns |  62.236 ns | 37.036 ns | 33.27 |    0.61 | 0.0191 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,195.09 ns |  12.310 ns |  8.142 ns | 33.07 |    0.49 | 0.0191 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   442.87 ns |   0.990 ns |  0.589 ns |  4.58 |    0.07 | 0.0062 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    96.65 ns |   2.285 ns |  1.511 ns |  1.00 |    0.02 | 0.0026 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 8,301.32 ns |  62.384 ns | 41.263 ns | 85.91 |    1.33 | 0.0458 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3,190.18 ns |  26.624 ns | 17.610 ns | 33.01 |    0.52 | 0.0191 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   377.55 ns |   4.401 ns |  2.619 ns |  3.91 |    0.06 | 0.0062 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |       |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   369.47 ns |  30.436 ns |  1.668 ns |  3.88 |    0.03 | 0.0062 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   466.38 ns |  64.846 ns |  3.554 ns |  4.89 |    0.05 | 0.0048 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,878.98 ns |  83.143 ns |  4.557 ns | 19.72 |    0.16 | 0.0114 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,139.68 ns | 161.639 ns |  8.860 ns | 32.95 |    0.28 | 0.0191 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,149.02 ns | 173.264 ns |  9.497 ns | 33.05 |    0.28 | 0.0191 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   445.14 ns |  27.946 ns |  1.532 ns |  4.67 |    0.04 | 0.0062 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    95.29 ns |  16.167 ns |  0.886 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 8,394.27 ns | 890.770 ns | 48.826 ns | 88.10 |    0.84 | 0.0458 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 3,228.54 ns | 559.508 ns | 30.668 ns | 33.88 |    0.39 | 0.0191 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   374.48 ns |  29.024 ns |  1.591 ns |  3.93 |    0.03 | 0.0062 |     520 B |        2.32 |
