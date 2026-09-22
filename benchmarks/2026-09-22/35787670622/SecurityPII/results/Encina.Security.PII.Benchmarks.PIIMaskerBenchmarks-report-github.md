```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    415.69 ns |   3.242 ns |  2.144 ns |   4.39 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    573.61 ns |   1.236 ns |  0.647 ns |   6.06 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,654.29 ns |   3.676 ns |  1.923 ns |  28.03 |    0.12 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,641.90 ns |  13.513 ns |  8.042 ns |  49.03 |    0.22 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,455.54 ns |  18.088 ns | 10.764 ns |  47.06 |    0.22 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    490.75 ns |   4.278 ns |  2.830 ns |   5.18 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     94.68 ns |   0.610 ns |  0.403 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,639.51 ns |  48.725 ns | 28.995 ns | 112.37 |    0.54 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,436.65 ns |  17.516 ns | 11.585 ns |  46.86 |    0.22 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    427.74 ns |   1.836 ns |  1.214 ns |   4.52 |    0.02 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    408.95 ns |  45.437 ns |  2.491 ns |   4.31 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    577.86 ns |  22.919 ns |  1.256 ns |   6.09 |    0.01 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,637.46 ns | 187.700 ns | 10.288 ns |  27.80 |    0.10 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,487.61 ns | 219.698 ns | 12.042 ns |  47.30 |    0.13 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,284.01 ns | 299.631 ns | 16.424 ns |  45.15 |    0.16 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    503.22 ns |  11.631 ns |  0.638 ns |   5.30 |    0.01 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     94.88 ns |   2.824 ns |  0.155 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,449.30 ns | 374.581 ns | 20.532 ns | 110.13 |    0.24 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,624.86 ns | 797.224 ns | 43.699 ns |  48.74 |    0.40 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    410.73 ns |  15.868 ns |  0.870 ns |   4.33 |    0.01 | 0.0310 |     520 B |        2.32 |
