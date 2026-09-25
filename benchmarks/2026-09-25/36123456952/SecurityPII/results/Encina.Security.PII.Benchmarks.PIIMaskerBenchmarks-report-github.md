```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    418.67 ns |     4.718 ns |  3.120 ns |   4.22 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    592.37 ns |     3.268 ns |  1.709 ns |   5.97 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,731.83 ns |    22.404 ns | 13.333 ns |  27.51 |    0.16 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,481.04 ns |    13.413 ns |  8.872 ns |  45.13 |    0.19 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,358.76 ns |     9.393 ns |  5.589 ns |  43.90 |    0.17 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    514.42 ns |     4.209 ns |  2.784 ns |   5.18 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     99.29 ns |     0.651 ns |  0.388 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,863.55 ns |    46.172 ns | 30.540 ns | 109.41 |    0.50 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,552.39 ns |     9.835 ns |  5.853 ns |  45.85 |    0.18 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    443.91 ns |     2.042 ns |  1.215 ns |   4.47 |    0.02 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |              |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    418.38 ns |    69.541 ns |  3.812 ns |   4.22 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    593.15 ns |    30.755 ns |  1.686 ns |   5.98 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,725.42 ns |   537.484 ns | 29.461 ns |  27.49 |    0.34 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,405.50 ns |   253.139 ns | 13.875 ns |  44.43 |    0.38 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,373.48 ns |   658.146 ns | 36.075 ns |  44.11 |    0.47 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    516.21 ns |    23.157 ns |  1.269 ns |   5.21 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     99.17 ns |    16.738 ns |  0.917 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,684.68 ns | 1,390.580 ns | 76.222 ns | 107.75 |    1.09 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,521.59 ns | 1,204.614 ns | 66.029 ns |  45.60 |    0.68 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    427.70 ns |    10.798 ns |  0.592 ns |   4.31 |    0.03 | 0.0310 |     520 B |        2.32 |
