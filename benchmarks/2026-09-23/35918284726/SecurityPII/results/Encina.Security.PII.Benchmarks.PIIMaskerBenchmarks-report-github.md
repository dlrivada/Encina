```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    388.63 ns |   3.398 ns |  2.247 ns |   4.13 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    553.73 ns |   1.066 ns |  0.558 ns |   5.88 |    0.01 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,560.01 ns |   6.010 ns |  3.143 ns |  27.18 |    0.04 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,257.40 ns |   9.917 ns |  6.560 ns |  45.20 |    0.07 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,269.28 ns |   5.914 ns |  3.093 ns |  45.32 |    0.04 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    490.32 ns |   1.417 ns |  0.937 ns |   5.21 |    0.01 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     94.19 ns |   0.114 ns |  0.068 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,142.25 ns |  31.173 ns | 18.550 ns | 107.68 |    0.20 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,438.03 ns |  14.174 ns |  9.376 ns |  47.12 |    0.10 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    395.68 ns |   0.973 ns |  0.644 ns |   4.20 |    0.01 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    400.47 ns |  38.868 ns |  2.130 ns |   4.40 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    556.92 ns |  15.371 ns |  0.843 ns |   6.12 |    0.01 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,577.54 ns | 112.997 ns |  6.194 ns |  28.34 |    0.06 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,251.61 ns | 162.991 ns |  8.934 ns |  46.75 |    0.09 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,348.98 ns | 158.797 ns |  8.704 ns |  47.82 |    0.09 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    486.55 ns |  34.813 ns |  1.908 ns |   5.35 |    0.02 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     90.94 ns |   1.311 ns |  0.072 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,086.59 ns | 617.579 ns | 33.852 ns | 110.91 |    0.33 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,534.27 ns | 135.181 ns |  7.410 ns |  49.86 |    0.08 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    405.75 ns |  94.932 ns |  5.204 ns |   4.46 |    0.05 | 0.0310 |     520 B |        2.32 |
