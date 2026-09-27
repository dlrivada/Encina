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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    397.33 ns |   2.373 ns |  1.241 ns |   4.26 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    561.63 ns |   5.635 ns |  3.353 ns |   6.03 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,728.78 ns |  15.534 ns | 10.275 ns |  29.28 |    0.16 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,342.71 ns |  23.193 ns | 15.341 ns |  46.59 |    0.24 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,416.08 ns |  23.744 ns | 15.705 ns |  47.38 |    0.25 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    499.14 ns |  12.270 ns |  8.116 ns |   5.36 |    0.09 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     93.21 ns |   0.741 ns |  0.387 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,675.36 ns |  31.536 ns | 20.859 ns | 114.53 |    0.50 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,561.14 ns |   9.196 ns |  4.810 ns |  48.94 |    0.20 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    409.29 ns |   3.599 ns |  2.142 ns |   4.39 |    0.03 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    402.51 ns |  33.191 ns |  1.819 ns |   4.24 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    588.30 ns |   3.100 ns |  0.170 ns |   6.19 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,675.53 ns | 510.944 ns | 28.007 ns |  28.16 |    0.31 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,376.78 ns | 645.821 ns | 35.400 ns |  46.06 |    0.43 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,392.40 ns | 349.004 ns | 19.130 ns |  46.23 |    0.33 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    502.53 ns |  46.639 ns |  2.556 ns |   5.29 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     95.02 ns |  12.366 ns |  0.678 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,781.10 ns | 934.893 ns | 51.245 ns | 113.47 |    0.84 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,525.08 ns | 131.668 ns |  7.217 ns |  47.62 |    0.30 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    408.93 ns |  20.472 ns |  1.122 ns |   4.30 |    0.03 | 0.0310 |     520 B |        2.32 |
