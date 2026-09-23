```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.22GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    447.30 ns |   6.996 ns |  4.627 ns |   4.56 |    0.06 | 0.0205 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    627.26 ns |   9.911 ns |  6.556 ns |   6.40 |    0.08 | 0.0162 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,727.48 ns |   7.785 ns |  5.149 ns |  27.82 |    0.23 | 0.0381 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,724.93 ns |  19.690 ns | 11.717 ns |  48.19 |    0.41 | 0.0687 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,779.20 ns |  25.734 ns | 17.022 ns |  48.75 |    0.43 | 0.0687 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    546.30 ns |   2.449 ns |  1.620 ns |   5.57 |    0.05 | 0.0210 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     98.05 ns |   1.267 ns |  0.838 ns |   1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 11,109.22 ns |  47.800 ns | 25.000 ns | 113.31 |    0.96 | 0.1984 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,885.21 ns |  12.319 ns |  8.148 ns |  49.83 |    0.42 | 0.0687 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    467.01 ns |  10.215 ns |  6.756 ns |   4.76 |    0.08 | 0.0205 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    456.77 ns |  23.061 ns |  1.264 ns |   4.66 |    0.04 | 0.0205 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    676.31 ns |  54.042 ns |  2.962 ns |   6.90 |    0.07 | 0.0162 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,872.09 ns | 203.968 ns | 11.180 ns |  29.28 |    0.28 | 0.0381 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,748.04 ns | 597.300 ns | 32.740 ns |  48.41 |    0.52 | 0.0687 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,933.93 ns | 971.828 ns | 53.269 ns |  50.31 |    0.65 | 0.0687 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    514.72 ns |  91.763 ns |  5.030 ns |   5.25 |    0.06 | 0.0210 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     98.09 ns |  18.560 ns |  1.017 ns |   1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 11,005.83 ns | 717.845 ns | 39.348 ns | 112.21 |    1.06 | 0.1984 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,902.34 ns | 716.653 ns | 39.282 ns |  49.98 |    0.57 | 0.0687 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    479.59 ns |  12.235 ns |  0.671 ns |   4.89 |    0.04 | 0.0200 |     520 B |        2.32 |
