```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    398.03 ns |   3.156 ns |  1.651 ns |   4.28 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    559.83 ns |   0.770 ns |  0.403 ns |   6.01 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,613.65 ns |  14.628 ns |  8.705 ns |  28.08 |    0.11 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,288.19 ns |  16.235 ns | 10.739 ns |  46.07 |    0.16 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,401.81 ns |  19.096 ns | 11.364 ns |  47.29 |    0.16 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    486.64 ns |   4.541 ns |  3.003 ns |   5.23 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     93.08 ns |   0.354 ns |  0.234 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,579.61 ns |  69.317 ns | 45.849 ns | 113.66 |    0.54 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,570.68 ns |  22.463 ns | 14.858 ns |  49.11 |    0.19 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    404.41 ns |   2.533 ns |  1.675 ns |   4.34 |    0.02 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    397.58 ns |  24.572 ns |  1.347 ns |   4.28 |    0.01 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    561.14 ns |  25.345 ns |  1.389 ns |   6.04 |    0.01 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,613.18 ns | 377.579 ns | 20.696 ns |  28.11 |    0.19 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,297.57 ns | 214.781 ns | 11.773 ns |  46.22 |    0.11 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,221.57 ns | 179.800 ns |  9.855 ns |  45.40 |    0.10 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    492.78 ns |  45.944 ns |  2.518 ns |   5.30 |    0.02 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     92.98 ns |   1.467 ns |  0.080 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,632.49 ns | 717.020 ns | 39.302 ns | 114.36 |    0.38 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,379.31 ns | 823.234 ns | 45.124 ns |  47.10 |    0.42 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    409.03 ns |  32.425 ns |  1.777 ns |   4.40 |    0.02 | 0.0310 |     520 B |        2.32 |
