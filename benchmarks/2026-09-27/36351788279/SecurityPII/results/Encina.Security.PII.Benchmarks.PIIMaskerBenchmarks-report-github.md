```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   334.03 ns |   2.869 ns |  1.898 ns |   4.20 |    0.04 | 0.0062 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   446.97 ns |   4.568 ns |  2.719 ns |   5.62 |    0.05 | 0.0048 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,825.59 ns |  22.358 ns | 13.305 ns |  22.94 |    0.23 | 0.0114 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,080.87 ns |   8.676 ns |  5.163 ns |  38.71 |    0.28 | 0.0191 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,045.82 ns |   5.298 ns |  3.504 ns |  38.27 |    0.28 | 0.0191 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   422.67 ns |   3.406 ns |  2.253 ns |   5.31 |    0.05 | 0.0062 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    79.59 ns |   1.013 ns |  0.603 ns |   1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 8,039.50 ns |  45.530 ns | 30.115 ns | 101.02 |    0.81 | 0.0458 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3,107.15 ns |  15.680 ns |  9.331 ns |  39.04 |    0.30 | 0.0191 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   341.08 ns |   2.140 ns |  1.416 ns |   4.29 |    0.04 | 0.0062 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   342.98 ns |  17.828 ns |  0.977 ns |   4.25 |    0.02 | 0.0062 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   471.51 ns | 105.867 ns |  5.803 ns |   5.84 |    0.06 | 0.0048 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,816.66 ns |  34.517 ns |  1.892 ns |  22.49 |    0.06 | 0.0114 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,054.52 ns |  47.141 ns |  2.584 ns |  37.82 |    0.10 | 0.0191 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,057.19 ns |  25.166 ns |  1.379 ns |  37.85 |    0.10 | 0.0191 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   418.08 ns |  89.326 ns |  4.896 ns |   5.18 |    0.05 | 0.0062 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    80.76 ns |   4.374 ns |  0.240 ns |   1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 7,908.31 ns | 485.164 ns | 26.593 ns |  97.92 |    0.38 | 0.0458 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 3,084.18 ns |  85.346 ns |  4.678 ns |  38.19 |    0.11 | 0.0191 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   345.69 ns |  59.199 ns |  3.245 ns |   4.28 |    0.04 | 0.0062 |     520 B |        2.32 |
