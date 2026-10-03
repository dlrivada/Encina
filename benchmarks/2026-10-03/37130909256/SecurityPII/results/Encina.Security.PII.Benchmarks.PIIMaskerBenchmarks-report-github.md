```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   332.03 ns |   5.939 ns |  3.928 ns |  4.35 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   422.92 ns |   1.318 ns |  0.872 ns |  5.54 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 2,011.29 ns |  17.632 ns | 10.493 ns | 26.37 |    0.25 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,129.40 ns |  10.854 ns |  6.459 ns | 41.03 |    0.33 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,148.18 ns |   6.317 ns |  4.178 ns | 41.28 |    0.33 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   413.78 ns |   1.356 ns |  0.807 ns |  5.43 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    76.28 ns |   1.067 ns |  0.635 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 7,417.15 ns |  25.546 ns | 16.897 ns | 97.25 |    0.80 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3,226.80 ns |  10.001 ns |  6.615 ns | 42.31 |    0.34 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   347.34 ns |  12.311 ns |  8.143 ns |  4.55 |    0.11 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |       |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   333.40 ns |  48.597 ns |  2.664 ns |  4.32 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   424.51 ns |   4.598 ns |  0.252 ns |  5.50 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,961.85 ns | 255.120 ns | 13.984 ns | 25.40 |    0.19 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,121.95 ns | 129.121 ns |  7.078 ns | 40.41 |    0.20 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,124.49 ns | 139.685 ns |  7.657 ns | 40.45 |    0.20 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   406.24 ns |  14.796 ns |  0.811 ns |  5.26 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    77.25 ns |   7.395 ns |  0.405 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 7,346.12 ns | 329.660 ns | 18.070 ns | 95.10 |    0.48 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 3,264.38 ns | 702.311 ns | 38.496 ns | 42.26 |    0.47 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   367.03 ns |  34.102 ns |  1.869 ns |  4.75 |    0.03 | 0.0310 |     520 B |        2.32 |
