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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    409.9 ns |   2.81 ns |  1.86 ns |   3.88 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    577.4 ns |   2.15 ns |  1.43 ns |   5.47 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,766.0 ns |   3.65 ns |  2.17 ns |  26.20 |    0.05 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,423.0 ns |  10.73 ns |  7.10 ns |  41.90 |    0.10 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,326.5 ns |   7.78 ns |  4.63 ns |  40.99 |    0.09 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    505.5 ns |   3.07 ns |  2.03 ns |   4.79 |    0.02 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    105.6 ns |   0.36 ns |  0.21 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,571.4 ns |  53.83 ns | 32.03 ns | 100.15 |    0.35 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,588.8 ns |  10.18 ns |  6.74 ns |  43.47 |    0.10 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    437.3 ns |   2.67 ns |  1.40 ns |   4.14 |    0.01 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |           |          |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    415.9 ns |  12.27 ns |  0.67 ns |   4.15 |    0.01 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    595.1 ns |  16.04 ns |  0.88 ns |   5.94 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,642.7 ns | 166.79 ns |  9.14 ns |  26.37 |    0.11 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,472.6 ns | 287.98 ns | 15.78 ns |  44.63 |    0.18 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,291.9 ns | 174.23 ns |  9.55 ns |  42.83 |    0.14 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    520.0 ns |  23.86 ns |  1.31 ns |   5.19 |    0.02 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    100.2 ns |   5.59 ns |  0.31 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,842.8 ns | 278.50 ns | 15.27 ns | 108.20 |    0.32 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,643.6 ns |  18.12 ns |  0.99 ns |  46.34 |    0.12 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    423.0 ns |  38.66 ns |  2.12 ns |   4.22 |    0.02 | 0.0310 |     520 B |        2.32 |
