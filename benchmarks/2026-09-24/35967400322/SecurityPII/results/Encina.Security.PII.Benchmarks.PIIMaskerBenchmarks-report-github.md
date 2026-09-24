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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    421.24 ns |  10.089 ns |  6.673 ns |   4.27 |    0.07 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    574.83 ns |   2.489 ns |  1.647 ns |   5.82 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,727.37 ns |   9.889 ns |  6.541 ns |  27.62 |    0.19 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,512.01 ns |  65.705 ns | 39.100 ns |  45.69 |    0.48 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,439.91 ns |  15.079 ns |  8.973 ns |  44.96 |    0.30 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    507.81 ns |   1.768 ns |  0.925 ns |   5.14 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     98.76 ns |   1.003 ns |  0.663 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,710.56 ns |  51.470 ns | 26.920 ns | 108.45 |    0.74 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,610.78 ns |  17.731 ns | 10.551 ns |  46.69 |    0.32 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    427.58 ns |   1.890 ns |  1.125 ns |   4.33 |    0.03 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    419.74 ns |  13.120 ns |  0.719 ns |   4.24 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    586.42 ns |   7.404 ns |  0.406 ns |   5.93 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,637.64 ns | 166.883 ns |  9.147 ns |  26.67 |    0.13 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,587.97 ns |  77.099 ns |  4.226 ns |  46.39 |    0.17 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,474.46 ns | 236.299 ns | 12.952 ns |  45.24 |    0.20 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    524.63 ns |  36.389 ns |  1.995 ns |   5.30 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     98.90 ns |   7.640 ns |  0.419 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,874.31 ns | 722.495 ns | 39.602 ns | 109.95 |    0.53 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,486.42 ns | 244.840 ns | 13.421 ns |  45.36 |    0.20 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    429.92 ns |  12.561 ns |  0.689 ns |   4.35 |    0.02 | 0.0310 |     520 B |        2.32 |
