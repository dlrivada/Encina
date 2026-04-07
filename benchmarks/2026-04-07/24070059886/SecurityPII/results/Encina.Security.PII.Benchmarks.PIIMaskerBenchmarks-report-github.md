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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        401.51 ns |  2.438 ns |  1.275 ns |   4.29 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        579.63 ns |  2.023 ns |  1.058 ns |   6.19 |    0.02 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,671.47 ns |  5.729 ns |  2.996 ns |  28.52 |    0.08 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,400.91 ns |  5.295 ns |  2.770 ns |  46.98 |    0.12 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,373.85 ns | 20.179 ns | 13.347 ns |  46.69 |    0.18 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        474.33 ns |  4.117 ns |  2.450 ns |   5.06 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         93.68 ns |  0.361 ns |  0.239 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,394.25 ns | 30.462 ns | 18.127 ns | 110.95 |    0.33 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,550.94 ns | 11.142 ns |  6.630 ns |  48.58 |    0.14 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        406.72 ns |  3.428 ns |  1.793 ns |   4.34 |    0.02 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |             |                  |           |           |        |         |        |           |             |
| Mask_SSN                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    359,692.00 ns |        NA |  0.000 ns |   1.02 |    0.00 |      - |     520 B |        2.32 |
| Mask_WithRegexPattern    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,173,259.00 ns |        NA |  0.000 ns |  37.30 |    0.00 |      - |     416 B |        1.86 |
| MaskObject_NoAttributes  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    288,159.00 ns |        NA |  0.000 ns |   0.82 |    0.00 |      - |    1008 B |        4.50 |
| MaskForAudit_SingleField | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    943,232.00 ns |        NA |  0.000 ns |   2.67 |    0.00 |      - |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,980,109.00 ns |        NA |  0.000 ns |   5.61 |    0.00 |      - |    1752 B |        7.82 |
| Mask_CreditCard          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    399,446.00 ns |        NA |  0.000 ns |   1.13 |    0.00 |      - |     544 B |        2.43 |
| Mask_Email               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    353,130.00 ns |        NA |  0.000 ns |   1.00 |    0.00 |      - |     224 B |        1.00 |
| MaskObject_MultiField    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    343,331.00 ns |        NA |  0.000 ns |   0.97 |    0.00 |      - |    5056 B |       22.57 |
| MaskObject_SingleField   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    292,707.00 ns |        NA |  0.000 ns |   0.83 |    0.00 |      - |    1752 B |        7.82 |
| Mask_Phone               | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    362,617.00 ns |        NA |  0.000 ns |   1.03 |    0.00 |      - |     520 B |        2.32 |
