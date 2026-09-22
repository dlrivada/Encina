```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   231.80 ns |     6.068 ns |  4.014 ns |   4.56 |    0.17 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   314.60 ns |     4.421 ns |  2.924 ns |   6.19 |    0.21 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,428.71 ns |    31.491 ns | 18.740 ns |  28.11 |    0.98 | 0.0591 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 2,279.56 ns |    28.356 ns | 18.756 ns |  44.86 |    1.50 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 2,242.58 ns |    65.114 ns | 43.069 ns |  44.13 |    1.65 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   279.39 ns |     9.546 ns |  6.314 ns |   5.50 |    0.21 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    50.87 ns |     2.649 ns |  1.752 ns |   1.00 |    0.05 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 5,373.07 ns |   130.531 ns | 86.338 ns | 105.73 |    3.80 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 2,267.68 ns |    73.096 ns | 48.349 ns |  44.62 |    1.71 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   245.05 ns |     6.463 ns |  3.846 ns |   4.82 |    0.17 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   226.14 ns |    94.615 ns |  5.186 ns |   4.42 |    0.11 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   324.07 ns |   205.483 ns | 11.263 ns |   6.33 |    0.22 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,433.76 ns |   361.785 ns | 19.831 ns |  28.02 |    0.56 | 0.0591 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 2,314.87 ns |   222.841 ns | 12.215 ns |  45.24 |    0.75 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 2,175.81 ns |   167.253 ns |  9.168 ns |  42.52 |    0.69 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   284.69 ns |    54.660 ns |  2.996 ns |   5.56 |    0.10 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    51.18 ns |    16.935 ns |  0.928 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 5,168.03 ns | 1,486.769 ns | 81.495 ns | 101.00 |    2.11 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 2,265.86 ns |   346.189 ns | 18.976 ns |  44.28 |    0.77 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   240.35 ns |   129.690 ns |  7.109 ns |   4.70 |    0.14 | 0.0310 |     520 B |        2.32 |
