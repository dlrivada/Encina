```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev     | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|-----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    416.74 ns |  4.227 ns |   2.796 ns |    417.25 ns |   3.66 |    0.05 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    603.12 ns |  4.907 ns |   2.920 ns |    603.01 ns |   5.30 |    0.07 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  2,705.03 ns |  9.053 ns |   5.988 ns |  2,703.95 ns |  23.76 |    0.28 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,446.01 ns |  8.870 ns |   4.639 ns |  4,445.64 ns |  39.05 |    0.45 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,393.25 ns | 18.334 ns |  12.127 ns |  4,392.35 ns |  38.58 |    0.45 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    495.58 ns |  0.934 ns |   0.489 ns |    495.53 ns |   4.35 |    0.05 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |    113.88 ns |  2.291 ns |   1.363 ns |    114.09 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,489.38 ns | 81.301 ns |  53.776 ns | 10,514.27 ns |  92.12 |    1.14 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  4,531.48 ns | 19.587 ns |  12.956 ns |  4,531.86 ns |  39.80 |    0.47 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    415.80 ns |  5.051 ns |   3.341 ns |    415.83 ns |   3.65 |    0.05 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |           |            |              |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    421.80 ns |  4.019 ns |   5.502 ns |    422.96 ns |   4.24 |    0.07 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    594.08 ns |  4.370 ns |   6.268 ns |    591.82 ns |   5.97 |    0.09 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,809.23 ns | 41.081 ns |  58.917 ns |  2,811.68 ns |  28.22 |    0.67 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,480.06 ns | 24.955 ns |  33.314 ns |  4,460.99 ns |  45.01 |    0.61 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,425.65 ns | 51.555 ns |  73.938 ns |  4,373.81 ns |  44.46 |    0.89 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    497.97 ns |  0.684 ns |   0.960 ns |    497.97 ns |   5.00 |    0.06 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |     99.55 ns |  0.805 ns |   1.154 ns |     99.78 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 10,630.48 ns | 81.085 ns | 118.853 ns | 10,573.75 ns | 106.80 |    1.70 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,674.55 ns | 12.224 ns |  17.136 ns |  4,672.33 ns |  46.97 |    0.56 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    419.57 ns |  3.864 ns |   5.784 ns |    420.26 ns |   4.22 |    0.07 | 0.0310 |     520 B |        2.32 |
