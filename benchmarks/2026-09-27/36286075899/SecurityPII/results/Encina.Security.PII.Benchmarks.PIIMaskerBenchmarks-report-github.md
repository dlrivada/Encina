```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error    | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|---------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    403.4 ns |  3.07 ns |  1.83 ns |   3.91 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    572.6 ns |  1.83 ns |  1.21 ns |   5.55 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  2,703.4 ns |  8.48 ns |  4.43 ns |  26.19 |    0.22 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,417.5 ns | 22.89 ns | 15.14 ns |  42.80 |    0.39 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,348.9 ns | 10.88 ns |  7.19 ns |  42.13 |    0.36 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    514.3 ns |  2.49 ns |  1.65 ns |   4.98 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |    103.2 ns |  1.38 ns |  0.91 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,629.8 ns | 50.76 ns | 33.57 ns | 102.98 |    0.92 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  4,583.4 ns | 18.54 ns | 12.26 ns |  44.41 |    0.39 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    418.8 ns |  3.11 ns |  2.06 ns |   4.06 |    0.04 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |             |          |          |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    416.4 ns |  2.55 ns |  3.82 ns |   4.15 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    596.4 ns |  9.42 ns | 14.10 ns |   5.95 |    0.15 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,695.0 ns |  3.49 ns |  4.89 ns |  26.88 |    0.21 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,400.2 ns | 30.14 ns | 44.18 ns |  43.88 |    0.54 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,398.5 ns | 14.03 ns | 20.57 ns |  43.87 |    0.38 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    513.8 ns |  5.24 ns |  7.51 ns |   5.12 |    0.08 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |    100.3 ns |  0.51 ns |  0.76 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 10,804.4 ns | 30.93 ns | 45.33 ns | 107.75 |    0.92 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,608.6 ns | 38.77 ns | 58.02 ns |  45.96 |    0.66 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    418.8 ns |  5.60 ns |  8.39 ns |   4.18 |    0.09 | 0.0310 |     520 B |        2.32 |
