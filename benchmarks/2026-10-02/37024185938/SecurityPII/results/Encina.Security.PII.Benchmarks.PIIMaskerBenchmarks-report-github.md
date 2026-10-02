```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    421.2 ns |    13.77 ns |  9.11 ns |   4.02 |    0.09 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    567.8 ns |     3.28 ns |  1.95 ns |   5.42 |    0.06 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,662.0 ns |    18.14 ns | 12.00 ns |  25.41 |    0.27 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,359.2 ns |    44.23 ns | 29.26 ns |  41.61 |    0.49 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,370.0 ns |    43.04 ns | 28.47 ns |  41.71 |    0.48 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    520.9 ns |     5.43 ns |  3.59 ns |   4.97 |    0.06 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    104.8 ns |     1.62 ns |  1.07 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,697.0 ns |    51.77 ns | 30.81 ns | 102.11 |    1.04 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,482.4 ns |     8.38 ns |  4.38 ns |  42.79 |    0.42 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    443.9 ns |     5.24 ns |  3.47 ns |   4.24 |    0.05 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |             |          |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    415.7 ns |   110.03 ns |  6.03 ns |   3.85 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    569.6 ns |    90.56 ns |  4.96 ns |   5.27 |    0.06 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,685.4 ns |   564.78 ns | 30.96 ns |  24.84 |    0.31 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,556.3 ns |   216.94 ns | 11.89 ns |  42.14 |    0.34 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,455.7 ns | 1,057.66 ns | 57.97 ns |  41.21 |    0.56 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    517.1 ns |    74.71 ns |  4.10 ns |   4.78 |    0.05 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    108.1 ns |    17.60 ns |  0.96 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,800.8 ns |   231.72 ns | 12.70 ns |  99.90 |    0.78 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,599.3 ns |   207.80 ns | 11.39 ns |  42.54 |    0.34 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    438.8 ns |    65.66 ns |  3.60 ns |   4.06 |    0.04 | 0.0310 |     520 B |        2.32 |
