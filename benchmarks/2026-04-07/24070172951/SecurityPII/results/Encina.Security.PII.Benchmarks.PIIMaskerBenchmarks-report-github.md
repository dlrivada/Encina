```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error     | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|----------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    413.8 ns |   1.57 ns |  1.04 ns |   4.07 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    591.1 ns |   1.93 ns |  1.15 ns |   5.81 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,647.4 ns |   7.53 ns |  4.98 ns |  26.01 |    0.16 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,512.4 ns |  14.05 ns |  7.35 ns |  44.33 |    0.27 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,384.9 ns |  13.43 ns |  7.99 ns |  43.08 |    0.26 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    494.8 ns |   2.02 ns |  1.20 ns |   4.86 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    101.8 ns |   0.94 ns |  0.62 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,694.7 ns |  28.77 ns | 19.03 ns | 105.07 |    0.64 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,650.1 ns |  24.04 ns | 15.90 ns |  45.68 |    0.30 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    423.4 ns |   3.59 ns |  2.13 ns |   4.16 |    0.03 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |           |          |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    422.2 ns |  34.37 ns |  1.88 ns |   4.18 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    597.2 ns |  43.94 ns |  2.41 ns |   5.92 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,780.1 ns | 172.70 ns |  9.47 ns |  27.55 |    0.14 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,572.9 ns | 576.45 ns | 31.60 ns |  45.32 |    0.33 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,459.2 ns | 453.55 ns | 24.86 ns |  44.19 |    0.28 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    499.0 ns |  54.80 ns |  3.00 ns |   4.95 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    100.9 ns |   8.44 ns |  0.46 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,471.2 ns | 450.82 ns | 24.71 ns | 103.77 |    0.46 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,686.9 ns | 635.42 ns | 34.83 ns |  46.45 |    0.35 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    429.7 ns |   6.91 ns |  0.38 ns |   4.26 |    0.02 | 0.0310 |     520 B |        2.32 |
