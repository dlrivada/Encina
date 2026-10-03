```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   322.80 ns |   2.375 ns |  1.413 ns |   4.39 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   414.29 ns |   2.860 ns |  1.892 ns |   5.63 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,932.95 ns |  10.971 ns |  7.257 ns |  26.27 |    0.10 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,079.89 ns |   5.799 ns |  3.836 ns |  41.86 |    0.06 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,036.19 ns |  10.234 ns |  6.769 ns |  41.27 |    0.09 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   395.91 ns |   4.412 ns |  2.625 ns |   5.38 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    73.58 ns |   0.100 ns |  0.060 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 7,359.04 ns |  27.178 ns | 17.977 ns | 100.02 |    0.25 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3,189.47 ns |   8.970 ns |  4.691 ns |  43.35 |    0.07 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   329.66 ns |   1.372 ns |  0.908 ns |   4.48 |    0.01 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   326.39 ns | 120.454 ns |  6.603 ns |   4.24 |    0.08 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   417.85 ns |  36.933 ns |  2.024 ns |   5.43 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,942.97 ns |  70.095 ns |  3.842 ns |  25.24 |    0.11 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,067.00 ns | 118.288 ns |  6.484 ns |  39.84 |    0.17 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,046.10 ns |  86.019 ns |  4.715 ns |  39.57 |    0.16 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   406.39 ns | 149.402 ns |  8.189 ns |   5.28 |    0.09 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    76.98 ns |   6.169 ns |  0.338 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 7,184.38 ns | 775.563 ns | 42.511 ns |  93.33 |    0.60 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 3,132.71 ns |  55.491 ns |  3.042 ns |  40.70 |    0.16 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   331.92 ns |  83.352 ns |  4.569 ns |   4.31 |    0.05 | 0.0310 |     520 B |        2.32 |
