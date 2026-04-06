```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_Email               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         97.65 ns |  0.486 ns |  0.321 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        412.85 ns |  5.316 ns |  3.516 ns |   4.23 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        487.52 ns |  1.300 ns |  0.680 ns |   4.99 |    0.02 | 0.0324 |     544 B |        2.43 |
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        409.26 ns |  3.476 ns |  2.299 ns |   4.19 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        595.32 ns |  2.231 ns |  1.328 ns |   6.10 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,474.63 ns | 17.119 ns | 11.323 ns |  45.82 |    0.18 | 0.0992 |    1752 B |        7.82 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,587.98 ns | 47.182 ns | 28.077 ns | 108.43 |    0.44 | 0.2899 |    5056 B |       22.57 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,694.98 ns |  4.580 ns |  3.029 ns |  27.60 |    0.09 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,535.20 ns | 16.851 ns | 11.146 ns |  46.44 |    0.18 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,484.46 ns | 14.164 ns |  7.408 ns |  45.92 |    0.16 | 0.0992 |    1752 B |        7.82 |
|                          |            |                |             |             |              |             |                  |           |           |        |         |        |           |             |
| Mask_Email               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    363,710.00 ns |        NA |  0.000 ns |   1.00 |    0.00 |      - |     224 B |        1.00 |
| Mask_Phone               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    402,373.00 ns |        NA |  0.000 ns |   1.11 |    0.00 |      - |   25144 B |      112.25 |
| Mask_CreditCard          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    371,895.00 ns |        NA |  0.000 ns |   1.02 |    0.00 |      - |     544 B |        2.43 |
| Mask_SSN                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    381,183.00 ns |        NA |  0.000 ns |   1.05 |    0.00 |      - |     520 B |        2.32 |
| Mask_WithRegexPattern    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,620,827.00 ns |        NA |  0.000 ns |  37.45 |    0.00 |      - |     416 B |        1.86 |
| MaskObject_SingleField   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    331,770.00 ns |        NA |  0.000 ns |   0.91 |    0.00 |      - |    1752 B |        7.82 |
| MaskObject_MultiField    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    368,560.00 ns |        NA |  0.000 ns |   1.01 |    0.00 |      - |    5056 B |       22.57 |
| MaskObject_NoAttributes  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    331,120.00 ns |        NA |  0.000 ns |   0.91 |    0.00 |      - |    1008 B |        4.50 |
| MaskForAudit_SingleField | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,049,855.00 ns |        NA |  0.000 ns |   2.89 |    0.00 |      - |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,103,065.00 ns |        NA |  0.000 ns |   5.78 |    0.00 |      - |    1752 B |        7.82 |
