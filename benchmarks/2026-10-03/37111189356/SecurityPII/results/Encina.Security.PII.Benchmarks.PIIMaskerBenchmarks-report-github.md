```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error     | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|----------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    408.2 ns |   0.83 ns |  0.55 ns |   3.99 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    568.2 ns |   2.27 ns |  1.50 ns |   5.55 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,617.3 ns |   7.57 ns |  5.00 ns |  25.57 |    0.11 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,364.4 ns |  11.34 ns |  7.50 ns |  42.63 |    0.18 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,297.8 ns |  14.09 ns |  9.32 ns |  41.98 |    0.18 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    507.9 ns |   3.18 ns |  1.89 ns |   4.96 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    102.4 ns |   0.68 ns |  0.41 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,558.5 ns |  31.83 ns | 21.06 ns | 103.14 |    0.44 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,433.9 ns |  27.61 ns | 18.26 ns |  43.31 |    0.24 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    414.8 ns |   1.87 ns |  1.24 ns |   4.05 |    0.02 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |           |          |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    403.1 ns |  42.02 ns |  2.30 ns |   3.97 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    560.2 ns |  24.64 ns |  1.35 ns |   5.51 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,694.9 ns | 109.08 ns |  5.98 ns |  26.52 |    0.13 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,502.6 ns | 325.46 ns | 17.84 ns |  44.32 |    0.25 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,340.1 ns | 143.90 ns |  7.89 ns |  42.72 |    0.20 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    506.9 ns | 297.32 ns | 16.30 ns |   4.99 |    0.14 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    101.6 ns |   9.32 ns |  0.51 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,883.8 ns | 303.93 ns | 16.66 ns | 107.12 |    0.49 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,662.1 ns | 499.80 ns | 27.40 ns |  45.89 |    0.31 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    429.5 ns |  19.72 ns |  1.08 ns |   4.23 |    0.02 | 0.0310 |     520 B |        2.32 |
