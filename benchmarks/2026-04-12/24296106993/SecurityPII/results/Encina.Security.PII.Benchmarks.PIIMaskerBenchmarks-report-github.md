```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.42GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    410.27 ns |  1.387 ns |  0.826 ns |   4.17 |    0.01 | 0.0205 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    629.91 ns |  3.230 ns |  1.922 ns |   6.40 |    0.02 | 0.0162 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  3,006.50 ns |  6.725 ns |  4.002 ns |  30.56 |    0.08 | 0.0381 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,790.27 ns | 20.444 ns | 13.522 ns |  48.69 |    0.17 | 0.0687 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,736.15 ns | 19.380 ns | 12.819 ns |  48.14 |    0.16 | 0.0687 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    514.54 ns |  1.466 ns |  0.970 ns |   5.23 |    0.01 | 0.0210 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |     98.38 ns |  0.387 ns |  0.231 ns |   1.00 |    0.00 | 0.0088 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,988.73 ns | 33.075 ns | 19.682 ns | 111.70 |    0.31 | 0.1984 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  5,034.74 ns | 13.772 ns |  9.109 ns |  51.18 |    0.14 | 0.0687 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    420.06 ns |  1.196 ns |  0.712 ns |   4.27 |    0.01 | 0.0205 |     520 B |        2.32 |
|                          |            |                |             |             |              |           |           |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    410.04 ns |  0.935 ns |  1.399 ns |   4.08 |    0.02 | 0.0205 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    647.00 ns |  8.336 ns | 11.955 ns |   6.44 |    0.12 | 0.0162 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,923.40 ns |  5.307 ns |  7.779 ns |  29.08 |    0.14 | 0.0381 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,737.35 ns | 15.484 ns | 22.697 ns |  47.12 |    0.29 | 0.0687 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,692.99 ns | 24.253 ns | 35.550 ns |  46.68 |    0.39 | 0.0687 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    487.35 ns |  2.437 ns |  3.647 ns |   4.85 |    0.04 | 0.0210 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |    100.55 ns |  0.285 ns |  0.400 ns |   1.00 |    0.01 | 0.0088 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 11,012.42 ns | 17.133 ns | 24.018 ns | 109.53 |    0.49 | 0.1984 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,869.74 ns | 11.022 ns | 15.808 ns |  48.43 |    0.24 | 0.0687 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    422.31 ns |  2.322 ns |  3.330 ns |   4.20 |    0.04 | 0.0205 |     520 B |        2.32 |
