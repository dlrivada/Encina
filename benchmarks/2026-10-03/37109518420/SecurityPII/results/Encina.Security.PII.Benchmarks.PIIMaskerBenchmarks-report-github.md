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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    421.7 ns |   3.58 ns |  2.13 ns |   3.96 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    572.9 ns |   1.52 ns |  0.91 ns |   5.38 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,681.4 ns |  14.40 ns |  7.53 ns |  25.19 |    0.12 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,459.3 ns |   9.48 ns |  4.96 ns |  41.90 |    0.16 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,296.5 ns |  11.40 ns |  6.79 ns |  40.37 |    0.16 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    513.8 ns |   3.01 ns |  1.99 ns |   4.83 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    106.4 ns |   0.63 ns |  0.42 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,700.1 ns |  19.33 ns | 12.79 ns | 100.54 |    0.39 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,520.4 ns |  13.31 ns |  8.81 ns |  42.47 |    0.18 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    429.4 ns |   1.63 ns |  1.08 ns |   4.04 |    0.02 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |           |          |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    423.6 ns |  19.96 ns |  1.09 ns |   3.91 |    0.01 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    566.4 ns |  14.46 ns |  0.79 ns |   5.23 |    0.01 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,697.9 ns |  57.28 ns |  3.14 ns |  24.90 |    0.05 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,634.5 ns | 214.41 ns | 11.75 ns |  42.78 |    0.12 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,668.2 ns | 195.09 ns | 10.69 ns |  43.09 |    0.11 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    514.7 ns |  76.08 ns |  4.17 ns |   4.75 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    108.3 ns |   3.78 ns |  0.21 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 11,062.6 ns | 800.58 ns | 43.88 ns | 102.12 |    0.39 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,555.8 ns |  86.73 ns |  4.75 ns |  42.05 |    0.08 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    435.6 ns |   9.49 ns |  0.52 ns |   4.02 |    0.01 | 0.0310 |     520 B |        2.32 |
