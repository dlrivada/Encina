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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    419.8 ns |   9.55 ns |  6.31 ns |   3.84 |    0.09 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    679.4 ns |   5.49 ns |  3.63 ns |   6.22 |    0.12 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,637.0 ns |   9.17 ns |  6.06 ns |  24.13 |    0.47 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,371.7 ns |  14.77 ns |  9.77 ns |  40.01 |    0.77 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,416.3 ns |  13.85 ns |  8.24 ns |  40.42 |    0.78 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    514.4 ns |   7.15 ns |  4.73 ns |   4.71 |    0.10 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    109.3 ns |   3.35 ns |  2.21 ns |   1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,535.9 ns |  50.81 ns | 33.60 ns |  96.43 |    1.88 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,504.8 ns |  14.37 ns |  9.51 ns |  41.23 |    0.80 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    431.1 ns |   7.70 ns |  5.09 ns |   3.95 |    0.09 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |           |          |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    416.5 ns |  27.52 ns |  1.51 ns |   3.89 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    569.3 ns |   1.13 ns |  0.06 ns |   5.32 |    0.07 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,797.6 ns | 215.43 ns | 11.81 ns |  26.13 |    0.34 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,528.6 ns | 609.50 ns | 33.41 ns |  42.30 |    0.59 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,481.7 ns | 807.11 ns | 44.24 ns |  41.86 |    0.63 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    517.4 ns |  34.56 ns |  1.89 ns |   4.83 |    0.06 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    107.1 ns |  28.09 ns |  1.54 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,901.9 ns | 570.12 ns | 31.25 ns | 101.83 |    1.29 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,641.3 ns | 392.34 ns | 21.51 ns |  43.35 |    0.56 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    439.6 ns |  47.76 ns |  2.62 ns |   4.11 |    0.06 | 0.0310 |     520 B |        2.32 |
