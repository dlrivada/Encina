```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   236.22 ns |   3.626 ns |  2.158 ns |   4.48 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   319.30 ns |   3.244 ns |  1.697 ns |   6.05 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,449.11 ns |  19.299 ns | 12.765 ns |  27.47 |    0.28 | 0.0591 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 2,310.89 ns |  23.606 ns | 12.346 ns |  43.80 |    0.33 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 2,360.33 ns |  29.814 ns | 15.593 ns |  44.74 |    0.38 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   288.08 ns |   5.352 ns |  3.540 ns |   5.46 |    0.07 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    52.76 ns |   0.607 ns |  0.317 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 5,408.99 ns |  59.977 ns | 35.691 ns | 102.52 |    0.87 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 2,416.24 ns |  33.808 ns | 22.362 ns |  45.80 |    0.48 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   246.17 ns |   4.770 ns |  3.155 ns |   4.67 |    0.06 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   234.63 ns |  40.600 ns |  2.225 ns |   4.49 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   318.60 ns |  22.016 ns |  1.207 ns |   6.10 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,473.55 ns | 116.695 ns |  6.396 ns |  28.20 |    0.20 | 0.0591 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 2,312.96 ns |  67.680 ns |  3.710 ns |  44.27 |    0.27 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 2,262.14 ns | 201.041 ns | 11.020 ns |  43.30 |    0.31 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   287.08 ns |  65.347 ns |  3.582 ns |   5.49 |    0.07 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    52.25 ns |   6.506 ns |  0.357 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 5,563.24 ns | 850.790 ns | 46.635 ns | 106.49 |    1.00 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 2,383.27 ns | 398.132 ns | 21.823 ns |  45.62 |    0.45 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   236.02 ns |  23.482 ns |  1.287 ns |   4.52 |    0.03 | 0.0310 |     520 B |        2.32 |
