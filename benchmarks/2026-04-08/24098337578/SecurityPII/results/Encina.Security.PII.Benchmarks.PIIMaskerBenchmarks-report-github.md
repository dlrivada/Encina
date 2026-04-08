```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error    | StdDev    | Median      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|---------:|----------:|------------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    428.4 ns |  2.97 ns |   1.96 ns |    428.3 ns |   4.00 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    587.7 ns |  3.45 ns |   2.28 ns |    586.9 ns |   5.48 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  2,683.0 ns |  7.31 ns |   4.83 ns |  2,682.8 ns |  25.04 |    0.12 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,504.7 ns | 12.69 ns |   8.39 ns |  4,505.3 ns |  42.04 |    0.20 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,375.7 ns | 15.41 ns |   9.17 ns |  4,376.4 ns |  40.83 |    0.20 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    513.5 ns |  3.79 ns |   2.51 ns |    512.4 ns |   4.79 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |    107.2 ns |  0.76 ns |   0.50 ns |    107.1 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,682.7 ns | 33.46 ns |  22.13 ns | 10,680.6 ns |  99.69 |    0.48 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  4,552.1 ns | 13.85 ns |   8.24 ns |  4,550.0 ns |  42.48 |    0.20 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    431.3 ns |  5.52 ns |   3.65 ns |    430.5 ns |   4.02 |    0.04 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |             |          |           |             |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    427.4 ns |  6.12 ns |   9.17 ns |    428.9 ns |   3.97 |    0.09 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    605.8 ns |  2.16 ns |   3.23 ns |    605.5 ns |   5.63 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,674.2 ns |  8.71 ns |  11.62 ns |  2,676.0 ns |  24.85 |    0.20 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,641.4 ns | 49.42 ns |  72.44 ns |  4,686.9 ns |  43.13 |    0.72 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,606.6 ns | 76.34 ns | 111.89 ns |  4,690.0 ns |  42.80 |    1.06 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    522.3 ns |  1.51 ns |   2.21 ns |    522.1 ns |   4.85 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |    107.6 ns |  0.51 ns |   0.75 ns |    107.7 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 10,883.4 ns | 27.22 ns |  39.90 ns | 10,881.7 ns | 101.12 |    0.78 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,610.2 ns | 41.17 ns |  59.05 ns |  4,610.9 ns |  42.84 |    0.61 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    432.0 ns |  2.31 ns |   3.46 ns |    432.1 ns |   4.01 |    0.04 | 0.0310 |     520 B |        2.32 |
