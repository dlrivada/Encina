```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    397.66 ns |     4.651 ns |  2.768 ns |   4.02 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    579.25 ns |     3.142 ns |  1.870 ns |   5.86 |    0.08 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,618.77 ns |    13.427 ns |  8.881 ns |  26.51 |    0.35 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,408.79 ns |    18.659 ns | 12.342 ns |  44.62 |    0.59 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,275.40 ns |    14.525 ns |  9.607 ns |  43.27 |    0.57 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    482.97 ns |     7.443 ns |  4.923 ns |   4.89 |    0.08 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     98.82 ns |     2.020 ns |  1.336 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,386.10 ns |    46.697 ns | 30.887 ns | 105.12 |    1.39 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,523.80 ns |    20.185 ns | 12.012 ns |  45.79 |    0.60 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    427.36 ns |     2.747 ns |  1.817 ns |   4.33 |    0.06 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |              |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    410.27 ns |     4.529 ns |  0.248 ns |   4.25 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    575.86 ns |    16.698 ns |  0.915 ns |   5.96 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,622.43 ns |    82.188 ns |  4.505 ns |  27.14 |    0.20 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,579.60 ns |    94.297 ns |  5.169 ns |  47.40 |    0.35 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,361.15 ns |   295.861 ns | 16.217 ns |  45.14 |    0.36 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    498.49 ns |    34.927 ns |  1.914 ns |   5.16 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     96.61 ns |    14.782 ns |  0.810 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,894.59 ns | 1,083.879 ns | 59.411 ns | 112.77 |    0.98 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,489.69 ns |   369.583 ns | 20.258 ns |  46.47 |    0.38 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    421.13 ns |    29.739 ns |  1.630 ns |   4.36 |    0.03 | 0.0310 |     520 B |        2.32 |
