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
| Mask_Email               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         99.30 ns |  4.569 ns |  3.022 ns |   1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        415.70 ns |  5.379 ns |  3.201 ns |   4.19 |    0.12 | 0.0310 |     520 B |        2.32 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        483.26 ns |  5.038 ns |  3.333 ns |   4.87 |    0.14 | 0.0324 |     544 B |        2.43 |
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        412.13 ns |  4.431 ns |  2.931 ns |   4.15 |    0.12 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        584.07 ns |  4.054 ns |  2.681 ns |   5.89 |    0.17 | 0.0248 |     416 B |        1.86 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,412.40 ns | 19.926 ns | 11.857 ns |  44.47 |    1.29 | 0.0992 |    1752 B |        7.82 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,363.08 ns | 67.044 ns | 39.897 ns | 104.44 |    3.04 | 0.2899 |    5056 B |       22.57 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,710.96 ns | 11.878 ns |  7.069 ns |  27.32 |    0.79 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,411.67 ns |  9.774 ns |  5.112 ns |  44.46 |    1.29 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,268.60 ns |  9.994 ns |  5.227 ns |  43.02 |    1.24 | 0.0992 |    1752 B |        7.82 |
|                          |            |                |             |             |              |             |                  |           |           |        |         |        |           |             |
| Mask_Email               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    425,085.00 ns |        NA |  0.000 ns |   1.00 |    0.00 |      - |     224 B |        1.00 |
| Mask_Phone               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    396,020.00 ns |        NA |  0.000 ns |   0.93 |    0.00 |      - |     520 B |        2.32 |
| Mask_CreditCard          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    418,943.00 ns |        NA |  0.000 ns |   0.99 |    0.00 |      - |     544 B |        2.43 |
| Mask_SSN                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    411,570.00 ns |        NA |  0.000 ns |   0.97 |    0.00 |      - |     520 B |        2.32 |
| Mask_WithRegexPattern    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,584,965.00 ns |        NA |  0.000 ns |  31.96 |    0.00 |      - |     416 B |        1.86 |
| MaskObject_SingleField   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    311,734.00 ns |        NA |  0.000 ns |   0.73 |    0.00 |      - |    1752 B |        7.82 |
| MaskObject_MultiField    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    434,964.00 ns |        NA |  0.000 ns |   1.02 |    0.00 |      - |    5056 B |       22.57 |
| MaskObject_NoAttributes  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    322,154.00 ns |        NA |  0.000 ns |   0.76 |    0.00 |      - |    1008 B |        4.50 |
| MaskForAudit_SingleField | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    969,967.00 ns |        NA |  0.000 ns |   2.28 |    0.00 |      - |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,191,524.00 ns |        NA |  0.000 ns |   5.16 |    0.00 |      - |    1752 B |        7.82 |
