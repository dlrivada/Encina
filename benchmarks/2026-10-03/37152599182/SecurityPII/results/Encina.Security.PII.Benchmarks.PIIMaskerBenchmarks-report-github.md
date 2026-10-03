```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    408.9 ns |     7.28 ns |   4.81 ns |   3.73 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    572.5 ns |     3.90 ns |   2.58 ns |   5.22 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,654.9 ns |    16.13 ns |   8.44 ns |  24.21 |    0.10 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,319.1 ns |    16.23 ns |  10.74 ns |  39.38 |    0.14 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,289.8 ns |    13.57 ns |   7.10 ns |  39.12 |    0.12 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    515.1 ns |     4.30 ns |   2.56 ns |   4.70 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    109.7 ns |     0.58 ns |   0.30 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,912.4 ns |    37.61 ns |  22.38 ns |  99.50 |    0.32 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,598.1 ns |    22.66 ns |  11.85 ns |  41.93 |    0.15 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    427.3 ns |     6.94 ns |   4.13 ns |   3.90 |    0.04 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |             |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    436.5 ns |    65.38 ns |   3.58 ns |   4.11 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    586.2 ns |    18.60 ns |   1.02 ns |   5.52 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,784.3 ns |    47.97 ns |   2.63 ns |  26.23 |    0.19 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,516.6 ns | 1,141.91 ns |  62.59 ns |  42.55 |    0.60 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,472.0 ns |   139.30 ns |   7.64 ns |  42.13 |    0.31 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    517.5 ns |    16.50 ns |   0.90 ns |   4.87 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    106.2 ns |    16.35 ns |   0.90 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 11,079.4 ns | 4,660.37 ns | 255.45 ns | 104.37 |    2.22 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,450.4 ns |   184.51 ns |  10.11 ns |  41.92 |    0.32 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    432.3 ns |    53.54 ns |   2.93 ns |   4.07 |    0.04 | 0.0310 |     520 B |        2.32 |
