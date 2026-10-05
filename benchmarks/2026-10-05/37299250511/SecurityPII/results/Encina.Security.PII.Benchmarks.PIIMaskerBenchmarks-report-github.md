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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   329.03 ns |   5.060 ns |  3.011 ns |  4.40 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   426.77 ns |   3.451 ns |  2.283 ns |  5.71 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,960.18 ns |  21.128 ns | 13.975 ns | 26.22 |    0.27 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,184.87 ns |  25.111 ns | 16.609 ns | 42.61 |    0.39 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,094.52 ns |  20.781 ns | 12.367 ns | 41.40 |    0.36 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   414.78 ns |   3.619 ns |  2.394 ns |  5.55 |    0.05 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    74.75 ns |   0.918 ns |  0.607 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 7,329.45 ns |  72.944 ns | 48.248 ns | 98.06 |    0.98 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3,193.36 ns |  18.296 ns | 12.101 ns | 42.72 |    0.37 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   332.94 ns |   3.705 ns |  2.451 ns |  4.45 |    0.05 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |       |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   331.24 ns | 175.281 ns |  9.608 ns |  4.25 |    0.12 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   428.59 ns |  32.537 ns |  1.783 ns |  5.50 |    0.07 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,964.63 ns | 168.788 ns |  9.252 ns | 25.19 |    0.34 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,152.25 ns |  47.307 ns |  2.593 ns | 40.43 |    0.52 | 0.1030 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,086.70 ns | 188.330 ns | 10.323 ns | 39.58 |    0.52 | 0.1030 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   406.27 ns | 105.943 ns |  5.807 ns |  5.21 |    0.09 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    77.99 ns |  20.974 ns |  1.150 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 7,282.28 ns | 217.690 ns | 11.932 ns | 93.39 |    1.21 | 0.2975 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 3,142.72 ns |  25.748 ns |  1.411 ns | 40.30 |    0.52 | 0.1030 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   355.88 ns |  93.893 ns |  5.147 ns |  4.56 |    0.08 | 0.0310 |     520 B |        2.32 |
