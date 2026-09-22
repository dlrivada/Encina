```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   227.31 ns |     8.140 ns |   5.384 ns |   4.87 |    0.16 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   299.09 ns |     5.616 ns |   3.342 ns |   6.41 |    0.17 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,365.90 ns |     9.851 ns |   5.862 ns |  29.27 |    0.73 | 0.0591 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 2,202.98 ns |    45.503 ns |  27.078 ns |  47.21 |    1.29 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 2,165.56 ns |    25.056 ns |  16.573 ns |  46.41 |    1.19 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   274.13 ns |     5.165 ns |   3.416 ns |   5.87 |    0.16 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    46.69 ns |     1.848 ns |   1.222 ns |   1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 4,983.72 ns |    68.724 ns |  40.896 ns | 106.81 |    2.76 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 2,152.39 ns |    33.445 ns |  22.122 ns |  46.13 |    1.22 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   219.92 ns |     7.343 ns |   4.857 ns |   4.71 |    0.15 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |            |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   232.56 ns |    72.630 ns |   3.981 ns |   5.00 |    0.08 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   309.90 ns |   230.085 ns |  12.612 ns |   6.66 |    0.24 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,418.27 ns |    67.823 ns |   3.718 ns |  30.47 |    0.18 | 0.0591 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 2,270.71 ns | 2,277.159 ns | 124.819 ns |  48.79 |    2.34 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 2,214.18 ns |   419.869 ns |  23.014 ns |  47.57 |    0.50 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   275.96 ns |   139.045 ns |   7.622 ns |   5.93 |    0.15 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    46.54 ns |     5.191 ns |   0.285 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 5,084.65 ns | 1,973.218 ns | 108.159 ns | 109.25 |    2.09 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 2,170.03 ns | 1,044.348 ns |  57.244 ns |  46.62 |    1.09 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   216.29 ns |    31.728 ns |   1.739 ns |   4.65 |    0.04 | 0.0310 |     520 B |        2.32 |
