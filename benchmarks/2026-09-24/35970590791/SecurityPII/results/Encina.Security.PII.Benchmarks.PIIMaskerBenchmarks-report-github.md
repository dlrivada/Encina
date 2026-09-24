```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   415.99 ns |   6.989 ns |  4.159 ns |   4.49 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   542.96 ns |   3.371 ns |  2.230 ns |   5.86 |    0.06 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 2,532.57 ns |  20.860 ns | 12.413 ns |  27.35 |    0.29 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,936.89 ns |  17.351 ns |  9.075 ns |  42.51 |    0.41 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,884.38 ns |  11.761 ns |  6.151 ns |  41.95 |    0.40 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   524.71 ns |  13.593 ns |  8.991 ns |   5.67 |    0.11 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    92.61 ns |   1.389 ns |  0.918 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 9,453.11 ns |  36.570 ns | 24.189 ns | 102.08 |    1.00 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 4,013.12 ns |  15.592 ns |  9.279 ns |  43.34 |    0.42 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   434.92 ns |  12.891 ns |  8.527 ns |   4.70 |    0.10 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   427.46 ns |  65.539 ns |  3.592 ns |   4.72 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   547.78 ns |  23.788 ns |  1.304 ns |   6.05 |    0.06 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 2,550.44 ns | 324.860 ns | 17.807 ns |  28.16 |    0.33 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 4,024.30 ns | 224.417 ns | 12.301 ns |  44.44 |    0.46 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,913.08 ns | 256.583 ns | 14.064 ns |  43.21 |    0.45 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   554.43 ns | 289.350 ns | 15.860 ns |   6.12 |    0.16 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    90.57 ns |  18.958 ns |  1.039 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 9,629.04 ns | 384.516 ns | 21.077 ns | 106.33 |    1.07 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 4,066.17 ns | 276.765 ns | 15.170 ns |  44.90 |    0.47 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   437.21 ns | 131.482 ns |  7.207 ns |   4.83 |    0.08 | 0.0310 |     520 B |        2.32 |
