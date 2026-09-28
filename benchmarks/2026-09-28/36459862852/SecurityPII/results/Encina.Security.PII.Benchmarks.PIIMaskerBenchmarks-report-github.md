```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   412.51 ns |   2.444 ns |  1.454 ns |   4.47 |    0.08 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   533.46 ns |   2.922 ns |  1.739 ns |   5.78 |    0.10 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 2,470.73 ns |  13.003 ns |  8.601 ns |  26.76 |    0.46 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,911.96 ns |  14.443 ns |  9.553 ns |  42.38 |    0.73 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,864.53 ns |  17.212 ns | 10.243 ns |  41.86 |    0.72 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   512.29 ns |   4.359 ns |  2.280 ns |   5.55 |    0.10 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    92.34 ns |   2.503 ns |  1.655 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 9,311.39 ns |  56.492 ns | 37.366 ns | 100.86 |    1.76 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3,989.92 ns |  26.847 ns | 15.976 ns |  43.22 |    0.75 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   425.75 ns |   4.502 ns |  2.679 ns |   4.61 |    0.08 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   406.04 ns |  13.639 ns |  0.748 ns |   4.34 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   531.06 ns |  24.914 ns |  1.366 ns |   5.68 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 2,430.83 ns | 108.870 ns |  5.968 ns |  26.00 |    0.12 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,994.90 ns | 242.338 ns | 13.283 ns |  42.72 |    0.22 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,918.17 ns | 107.973 ns |  5.918 ns |  41.90 |    0.18 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   525.96 ns | 147.056 ns |  8.061 ns |   5.62 |    0.08 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    93.51 ns |   8.118 ns |  0.445 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 9,380.17 ns | 263.863 ns | 14.463 ns | 100.32 |    0.44 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 4,018.64 ns |  92.692 ns |  5.081 ns |  42.98 |    0.18 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   428.86 ns | 147.859 ns |  8.105 ns |   4.59 |    0.08 | 0.0310 |     520 B |        2.32 |
