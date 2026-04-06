```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error    | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|---------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    413.1 ns |  3.49 ns |  2.31 ns |   3.82 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    590.6 ns |  0.75 ns |  0.39 ns |   5.46 |    0.07 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  2,651.1 ns |  8.56 ns |  5.10 ns |  24.51 |    0.31 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,549.4 ns | 18.51 ns | 12.24 ns |  42.07 |    0.53 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,350.4 ns | 23.64 ns | 14.07 ns |  40.23 |    0.51 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    475.7 ns |  1.91 ns |  1.00 ns |   4.40 |    0.06 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |    108.2 ns |  2.12 ns |  1.40 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,763.9 ns | 26.86 ns | 17.77 ns |  99.53 |    1.24 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  4,441.5 ns | 12.18 ns |  6.37 ns |  41.07 |    0.51 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    423.9 ns | 12.25 ns |  7.29 ns |   3.92 |    0.08 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |             |          |          |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    415.7 ns |  5.52 ns |  8.26 ns |   4.04 |    0.08 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    588.4 ns |  6.57 ns |  9.84 ns |   5.72 |    0.10 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,652.4 ns | 10.96 ns | 16.07 ns |  25.79 |    0.21 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,492.8 ns | 65.09 ns | 97.42 ns |  43.69 |    0.96 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,406.9 ns | 45.62 ns | 62.44 ns |  42.86 |    0.64 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    485.5 ns |  4.99 ns |  7.47 ns |   4.72 |    0.08 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |    102.8 ns |  0.39 ns |  0.59 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 10,628.5 ns | 60.93 ns | 87.39 ns | 103.36 |    1.02 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,524.3 ns | 24.46 ns | 36.61 ns |  44.00 |    0.43 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    414.4 ns |  6.71 ns | 10.05 ns |   4.03 |    0.10 | 0.0310 |     520 B |        2.32 |
