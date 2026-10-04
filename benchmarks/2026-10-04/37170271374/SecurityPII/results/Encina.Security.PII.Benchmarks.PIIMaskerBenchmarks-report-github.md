```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error    | StdDev   | Median      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|---------:|---------:|------------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    412.5 ns |  3.24 ns |  2.14 ns |    412.1 ns |   3.78 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    582.7 ns |  4.90 ns |  3.24 ns |    581.2 ns |   5.34 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  2,674.5 ns |  6.97 ns |  4.15 ns |  2,674.3 ns |  24.52 |    0.20 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,466.4 ns |  8.86 ns |  4.64 ns |  4,467.3 ns |  40.95 |    0.33 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,474.5 ns | 12.58 ns |  7.49 ns |  4,472.6 ns |  41.02 |    0.34 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    513.5 ns |  5.35 ns |  3.54 ns |    511.9 ns |   4.71 |    0.05 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |    109.1 ns |  1.40 ns |  0.92 ns |    109.2 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,757.7 ns | 18.13 ns | 10.79 ns | 10,756.4 ns |  98.63 |    0.80 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  4,457.1 ns | 14.59 ns |  9.65 ns |  4,456.1 ns |  40.86 |    0.34 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    425.1 ns |  9.47 ns |  6.26 ns |    424.2 ns |   3.90 |    0.06 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |             |          |          |             |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    420.5 ns |  2.70 ns |  4.05 ns |    419.9 ns |   3.84 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    594.8 ns |  6.22 ns |  9.12 ns |    600.7 ns |   5.43 |    0.11 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,700.0 ns | 21.87 ns | 31.37 ns |  2,718.9 ns |  24.65 |    0.42 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,475.5 ns | 52.16 ns | 74.81 ns |  4,479.9 ns |  40.86 |    0.84 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,499.8 ns | 27.52 ns | 39.47 ns |  4,500.2 ns |  41.09 |    0.62 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    518.3 ns |  2.38 ns |  3.48 ns |    517.8 ns |   4.73 |    0.07 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |    109.5 ns |  0.92 ns |  1.38 ns |    109.9 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 11,049.9 ns | 24.64 ns | 35.34 ns | 11,056.1 ns | 100.89 |    1.29 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,478.4 ns | 15.38 ns | 22.55 ns |  4,474.9 ns |  40.89 |    0.55 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    435.0 ns |  1.58 ns |  2.31 ns |    435.4 ns |   3.97 |    0.05 | 0.0310 |     520 B |        2.32 |
