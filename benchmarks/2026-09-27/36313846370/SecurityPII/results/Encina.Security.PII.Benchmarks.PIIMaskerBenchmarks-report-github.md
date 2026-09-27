```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    400.48 ns |     4.991 ns |  3.301 ns |   4.08 |    0.12 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    584.57 ns |    10.242 ns |  6.775 ns |   5.95 |    0.19 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,639.47 ns |    22.270 ns | 14.730 ns |  26.87 |    0.81 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,456.90 ns |    28.878 ns | 19.101 ns |  45.37 |    1.35 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,440.71 ns |    39.535 ns | 23.527 ns |  45.20 |    1.36 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    500.10 ns |     4.568 ns |  3.022 ns |   5.09 |    0.15 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     98.32 ns |     4.574 ns |  3.026 ns |   1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,776.29 ns |    93.648 ns | 61.942 ns | 109.70 |    3.30 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,580.88 ns |    41.941 ns | 24.958 ns |  46.63 |    1.40 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    418.95 ns |     9.944 ns |  6.578 ns |   4.26 |    0.14 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |              |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    430.56 ns |   101.494 ns |  5.563 ns |   4.32 |    0.10 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    565.29 ns |    76.448 ns |  4.190 ns |   5.67 |    0.12 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,697.66 ns |   237.623 ns | 13.025 ns |  27.04 |    0.53 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,464.98 ns |   301.068 ns | 16.503 ns |  44.76 |    0.87 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,514.38 ns |   145.122 ns |  7.955 ns |  45.25 |    0.87 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    499.74 ns |    64.463 ns |  3.533 ns |   5.01 |    0.10 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     99.79 ns |    39.979 ns |  2.191 ns |   1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,850.22 ns | 1,653.091 ns | 90.611 ns | 108.76 |    2.24 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,514.59 ns |   242.004 ns | 13.265 ns |  45.25 |    0.88 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    411.49 ns |    86.209 ns |  4.725 ns |   4.12 |    0.09 | 0.0310 |     520 B |        2.32 |
