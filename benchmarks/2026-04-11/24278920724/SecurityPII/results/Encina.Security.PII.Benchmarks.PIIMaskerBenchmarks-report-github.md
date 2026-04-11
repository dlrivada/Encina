```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    403.60 ns |  2.077 ns |  1.086 ns |    403.90 ns |   4.22 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    575.62 ns |  2.616 ns |  1.557 ns |    575.19 ns |   6.01 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  2,646.87 ns |  8.154 ns |  4.853 ns |  2,646.08 ns |  27.64 |    0.22 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,374.01 ns |  4.222 ns |  2.208 ns |  4,373.76 ns |  45.68 |    0.36 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,305.01 ns | 20.669 ns | 13.671 ns |  4,299.13 ns |  44.96 |    0.38 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    480.68 ns |  1.688 ns |  1.116 ns |    480.82 ns |   5.02 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |     95.75 ns |  1.189 ns |  0.787 ns |     95.49 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,487.38 ns | 37.114 ns | 24.548 ns | 10,479.31 ns | 109.53 |    0.89 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  4,520.29 ns | 12.538 ns |  8.293 ns |  4,519.40 ns |  47.21 |    0.38 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    406.73 ns |  2.413 ns |  1.596 ns |    406.71 ns |   4.25 |    0.04 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |           |           |              |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    412.73 ns |  2.927 ns |  4.381 ns |    412.20 ns |   4.22 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    575.91 ns |  1.765 ns |  2.641 ns |    575.30 ns |   5.88 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,695.76 ns | 31.759 ns | 47.536 ns |  2,706.04 ns |  27.54 |    0.50 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,581.66 ns | 13.860 ns | 20.315 ns |  4,589.46 ns |  46.81 |    0.32 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,572.03 ns |  8.062 ns | 11.302 ns |  4,569.40 ns |  46.71 |    0.27 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    486.72 ns |  2.368 ns |  3.472 ns |    488.11 ns |   4.97 |    0.04 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |     97.89 ns |  0.345 ns |  0.517 ns |     97.83 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 10,587.88 ns | 50.884 ns | 72.977 ns | 10,592.74 ns | 108.17 |    0.93 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,520.41 ns | 21.754 ns | 30.496 ns |  4,513.73 ns |  46.18 |    0.39 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    421.18 ns |  5.606 ns |  8.040 ns |    427.18 ns |   4.30 |    0.08 | 0.0310 |     520 B |        2.32 |
