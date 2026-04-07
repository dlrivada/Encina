```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    395.55 ns |  2.509 ns |  1.493 ns |   4.20 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    561.98 ns |  1.855 ns |  0.970 ns |   5.97 |    0.01 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  2,672.45 ns |  6.373 ns |  3.793 ns |  28.39 |    0.06 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,359.13 ns | 11.319 ns |  5.920 ns |  46.32 |    0.10 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,306.20 ns |  9.407 ns |  5.598 ns |  45.75 |    0.10 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    477.64 ns |  5.245 ns |  3.121 ns |   5.07 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |     94.12 ns |  0.322 ns |  0.168 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,268.66 ns | 33.434 ns | 22.115 ns | 109.11 |    0.29 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  4,528.44 ns | 20.216 ns | 13.372 ns |  48.12 |    0.16 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    402.68 ns |  2.191 ns |  1.304 ns |   4.28 |    0.01 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |           |           |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    411.59 ns |  1.411 ns |  2.112 ns |   4.16 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    568.59 ns |  1.874 ns |  2.805 ns |   5.75 |    0.06 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,667.54 ns |  8.749 ns | 12.548 ns |  26.98 |    0.26 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,515.78 ns | 17.664 ns | 25.892 ns |  45.67 |    0.46 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,393.42 ns | 46.799 ns | 70.046 ns |  44.44 |    0.79 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    482.09 ns |  2.923 ns |  4.284 ns |   4.88 |    0.06 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |     98.88 ns |  0.558 ns |  0.835 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 10,576.20 ns | 58.367 ns | 85.554 ns | 106.97 |    1.23 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,553.35 ns | 27.154 ns | 40.644 ns |  46.05 |    0.56 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    411.46 ns |  1.926 ns |  2.882 ns |   4.16 |    0.04 | 0.0310 |     520 B |        2.32 |
