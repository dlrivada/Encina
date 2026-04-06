```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     95.72 ns |   1.708 ns |  1.130 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    406.51 ns |   4.857 ns |  3.213 ns |   4.25 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    476.64 ns |   4.222 ns |  2.793 ns |   4.98 |    0.06 | 0.0324 |     544 B |        2.43 |
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    399.26 ns |   2.979 ns |  1.970 ns |   4.17 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    588.45 ns |   2.328 ns |  1.540 ns |   6.15 |    0.07 | 0.0248 |     416 B |        1.86 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,449.32 ns |  80.238 ns | 53.073 ns |  46.49 |    0.74 | 0.0992 |    1752 B |        7.82 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,386.65 ns |  37.261 ns | 24.646 ns | 108.53 |    1.25 | 0.2899 |    5056 B |       22.57 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,781.24 ns |   6.667 ns |  3.487 ns |  29.06 |    0.33 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,410.53 ns |  17.993 ns | 10.707 ns |  46.09 |    0.53 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,429.31 ns |  28.600 ns | 17.019 ns |  46.28 |    0.55 | 0.0992 |    1752 B |        7.82 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_Email               | ShortRun   | 3              | 1           |     96.75 ns |   7.539 ns |  0.413 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Mask_Phone               | ShortRun   | 3              | 1           |    409.53 ns |  20.170 ns |  1.106 ns |   4.23 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    478.57 ns |  23.610 ns |  1.294 ns |   4.95 |    0.02 | 0.0324 |     544 B |        2.43 |
| Mask_SSN                 | ShortRun   | 3              | 1           |    406.96 ns |  57.622 ns |  3.158 ns |   4.21 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    574.99 ns |  10.074 ns |  0.552 ns |   5.94 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,469.71 ns | 245.802 ns | 13.473 ns |  46.20 |    0.21 | 0.0992 |    1752 B |        7.82 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,564.16 ns | 468.824 ns | 25.698 ns | 109.20 |    0.46 | 0.2899 |    5056 B |       22.57 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,675.19 ns | 134.730 ns |  7.385 ns |  27.65 |    0.12 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,460.50 ns | 476.508 ns | 26.119 ns |  46.11 |    0.29 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,385.88 ns | 110.331 ns |  6.048 ns |  45.33 |    0.18 | 0.0992 |    1752 B |        7.82 |
