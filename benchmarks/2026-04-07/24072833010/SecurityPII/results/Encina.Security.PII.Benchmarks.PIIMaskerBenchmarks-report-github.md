```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        421.0 ns |  7.71 ns |  4.59 ns |   4.10 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        591.5 ns |  1.80 ns |  1.07 ns |   5.77 |    0.07 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,660.1 ns |  4.76 ns |  3.15 ns |  25.94 |    0.29 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,502.3 ns | 11.32 ns |  7.49 ns |  43.90 |    0.50 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,409.1 ns | 14.60 ns |  8.69 ns |  42.99 |    0.49 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        498.3 ns |  6.46 ns |  4.27 ns |   4.86 |    0.07 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        102.6 ns |  1.83 ns |  1.21 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,758.2 ns | 40.69 ns | 26.91 ns | 104.89 |    1.20 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,472.3 ns |  9.93 ns |  5.91 ns |  43.60 |    0.49 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        428.3 ns |  3.03 ns |  1.80 ns |   4.18 |    0.05 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |             |                 |          |          |        |         |        |           |             |
| Mask_SSN                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    433,443.0 ns |       NA |  0.00 ns |   1.17 |    0.00 |      - |     520 B |        2.32 |
| Mask_WithRegexPattern    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,188,344.0 ns |       NA |  0.00 ns |  38.24 |    0.00 |      - |     416 B |        1.86 |
| MaskObject_NoAttributes  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    351,469.0 ns |       NA |  0.00 ns |   0.95 |    0.00 |      - |    1008 B |        4.50 |
| MaskForAudit_SingleField | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    963,748.0 ns |       NA |  0.00 ns |   2.60 |    0.00 |      - |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,083,767.0 ns |       NA |  0.00 ns |   5.62 |    0.00 |      - |    1752 B |        7.82 |
| Mask_CreditCard          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    448,822.0 ns |       NA |  0.00 ns |   1.21 |    0.00 |      - |     544 B |        2.43 |
| Mask_Email               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    371,076.0 ns |       NA |  0.00 ns |   1.00 |    0.00 |      - |     224 B |        1.00 |
| MaskObject_MultiField    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    437,481.0 ns |       NA |  0.00 ns |   1.18 |    0.00 |      - |    5056 B |       22.57 |
| MaskObject_SingleField   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    300,073.0 ns |       NA |  0.00 ns |   0.81 |    0.00 |      - |    1752 B |        7.82 |
| Mask_Phone               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    439,624.0 ns |       NA |  0.00 ns |   1.18 |    0.00 |      - |     520 B |        2.32 |
