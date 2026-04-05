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
| Mask_Email               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         94.02 ns |  0.375 ns |  0.248 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        400.85 ns |  1.358 ns |  0.808 ns |   4.26 |    0.01 | 0.0310 |     520 B |        2.32 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        477.75 ns |  3.034 ns |  1.587 ns |   5.08 |    0.02 | 0.0324 |     544 B |        2.43 |
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        393.04 ns |  1.221 ns |  0.808 ns |   4.18 |    0.01 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        583.24 ns |  2.163 ns |  1.131 ns |   6.20 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,579.96 ns | 10.533 ns |  6.268 ns |  48.71 |    0.14 | 0.0992 |    1752 B |        7.82 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,700.60 ns | 29.034 ns | 17.278 ns | 113.81 |    0.34 | 0.2899 |    5056 B |       22.57 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,653.82 ns | 13.982 ns |  7.313 ns |  28.23 |    0.10 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,582.87 ns | 27.486 ns | 16.357 ns |  48.74 |    0.21 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,317.65 ns | 10.797 ns |  6.425 ns |  45.92 |    0.13 | 0.0992 |    1752 B |        7.82 |
|                          |            |                |             |             |              |             |                  |           |           |        |         |        |           |             |
| Mask_Email               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    356,404.00 ns |        NA |  0.000 ns |   1.00 |    0.00 |      - |     224 B |        1.00 |
| Mask_Phone               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    365,351.00 ns |        NA |  0.000 ns |   1.03 |    0.00 |      - |     520 B |        2.32 |
| Mask_CreditCard          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    402,801.00 ns |        NA |  0.000 ns |   1.13 |    0.00 |      - |     544 B |        2.43 |
| Mask_SSN                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    464,616.00 ns |        NA |  0.000 ns |   1.30 |    0.00 |      - |     520 B |        2.32 |
| Mask_WithRegexPattern    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,435,723.00 ns |        NA |  0.000 ns |  37.70 |    0.00 |      - |     416 B |        1.86 |
| MaskObject_SingleField   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    292,856.00 ns |        NA |  0.000 ns |   0.82 |    0.00 |      - |    1752 B |        7.82 |
| MaskObject_MultiField    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    363,397.00 ns |        NA |  0.000 ns |   1.02 |    0.00 |      - |    5056 B |       22.57 |
| MaskObject_NoAttributes  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    307,073.00 ns |        NA |  0.000 ns |   0.86 |    0.00 |      - |    1008 B |        4.50 |
| MaskForAudit_SingleField | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    889,918.00 ns |        NA |  0.000 ns |   2.50 |    0.00 |      - |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,974,178.00 ns |        NA |  0.000 ns |   5.54 |    0.00 |      - |    1752 B |        7.82 |
