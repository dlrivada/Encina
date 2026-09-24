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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   218.50 ns |  10.789 ns |  6.421 ns |   4.54 |    0.13 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   285.75 ns |   1.793 ns |  0.938 ns |   5.94 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,323.49 ns |   7.277 ns |  4.813 ns |  27.50 |    0.25 | 0.0591 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 2,121.91 ns |  23.694 ns | 14.100 ns |  44.09 |    0.46 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 2,090.80 ns |  18.891 ns | 12.495 ns |  43.45 |    0.44 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   271.22 ns |   6.099 ns |  3.629 ns |   5.64 |    0.09 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    48.13 ns |   0.639 ns |  0.422 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 5,303.75 ns |  49.503 ns | 25.891 ns | 110.21 |    1.05 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 2,387.21 ns |  32.015 ns | 19.052 ns |  49.61 |    0.56 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   243.45 ns |   4.785 ns |  3.165 ns |   5.06 |    0.08 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   207.15 ns | 170.974 ns |  9.372 ns |   4.22 |    0.17 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   294.81 ns |  54.274 ns |  2.975 ns |   6.01 |    0.08 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,352.65 ns | 109.552 ns |  6.005 ns |  27.56 |    0.31 | 0.0591 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 2,148.83 ns | 867.321 ns | 47.541 ns |  43.79 |    0.96 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 2,173.60 ns | 298.236 ns | 16.347 ns |  44.29 |    0.56 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   285.04 ns |  55.533 ns |  3.044 ns |   5.81 |    0.08 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    49.08 ns |  11.112 ns |  0.609 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 5,449.56 ns | 313.223 ns | 17.169 ns | 111.05 |    1.23 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 2,395.39 ns | 461.856 ns | 25.316 ns |  48.81 |    0.69 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   238.95 ns |  46.306 ns |  2.538 ns |   4.87 |    0.07 | 0.0310 |     520 B |        2.32 |
