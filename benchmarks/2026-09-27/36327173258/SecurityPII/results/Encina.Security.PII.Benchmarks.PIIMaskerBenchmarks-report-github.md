```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    461.20 ns |   2.539 ns |  1.679 ns |   4.79 |    0.04 | 0.0205 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    625.35 ns |   0.970 ns |  0.642 ns |   6.50 |    0.05 | 0.0162 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,747.81 ns |  12.709 ns |  7.563 ns |  28.55 |    0.23 | 0.0381 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,708.55 ns |  18.580 ns | 11.057 ns |  48.92 |    0.38 | 0.0687 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,930.88 ns |  17.916 ns | 11.850 ns |  51.23 |    0.40 | 0.0687 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    542.28 ns |   1.347 ns |  0.891 ns |   5.63 |    0.04 | 0.0210 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     96.25 ns |   1.147 ns |  0.759 ns |   1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 11,063.84 ns |  22.114 ns | 13.160 ns | 114.95 |    0.88 | 0.1984 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,897.95 ns |   4.291 ns |  2.244 ns |  50.89 |    0.38 | 0.0687 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    466.20 ns |   2.986 ns |  1.777 ns |   4.84 |    0.04 | 0.0205 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    458.73 ns |  19.787 ns |  1.085 ns |   4.73 |    0.10 | 0.0205 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    643.81 ns |  15.531 ns |  0.851 ns |   6.64 |    0.14 | 0.0162 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,901.37 ns |  16.033 ns |  0.879 ns |  29.93 |    0.65 | 0.0381 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,787.86 ns | 128.327 ns |  7.034 ns |  49.39 |    1.07 | 0.0687 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,842.59 ns | 607.714 ns | 33.311 ns |  49.96 |    1.12 | 0.0687 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    532.88 ns |   9.675 ns |  0.530 ns |   5.50 |    0.12 | 0.0210 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     96.97 ns |  44.867 ns |  2.459 ns |   1.00 |    0.03 | 0.0088 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 11,003.29 ns | 432.665 ns | 23.716 ns | 113.52 |    2.47 | 0.1984 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,939.48 ns | 477.189 ns | 26.156 ns |  50.96 |    1.13 | 0.0687 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    467.75 ns |  26.052 ns |  1.428 ns |   4.83 |    0.11 | 0.0205 |     520 B |        2.32 |
