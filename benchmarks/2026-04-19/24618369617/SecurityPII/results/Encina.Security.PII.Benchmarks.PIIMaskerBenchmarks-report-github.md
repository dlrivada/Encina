```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     | 3           |    402.86 ns |  2.889 ns |  1.911 ns |    402.46 ns |   4.06 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     | 3           |    582.49 ns |  3.312 ns |  1.971 ns |    582.46 ns |   5.87 |    0.06 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 3           |  2,588.77 ns | 13.482 ns |  8.023 ns |  2,586.13 ns |  26.10 |    0.27 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3           |  4,420.02 ns | 21.322 ns | 14.103 ns |  4,421.76 ns |  44.56 |    0.46 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3           |  4,260.93 ns | 12.122 ns |  7.214 ns |  4,263.99 ns |  42.96 |    0.43 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     | 3           |    518.67 ns |  3.679 ns |  2.433 ns |    518.93 ns |   5.23 |    0.06 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     | 3           |     99.20 ns |  1.536 ns |  1.016 ns |     99.59 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 3           | 10,602.92 ns | 56.153 ns | 37.142 ns | 10,603.17 ns | 106.89 |    1.11 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3           |  4,539.88 ns | 29.527 ns | 17.571 ns |  4,530.44 ns |  45.77 |    0.48 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     | 3           |    416.11 ns |  6.921 ns |  4.578 ns |    414.40 ns |   4.19 |    0.06 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |              |           |           |              |        |         |        |           |             |
| Mask_SSN                 | MediumRun  | 15             | 2           | 10          |    413.19 ns |  2.121 ns |  3.109 ns |    413.76 ns |   4.18 |    0.07 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | MediumRun  | 15             | 2           | 10          |    598.35 ns |  2.052 ns |  3.072 ns |    598.70 ns |   6.05 |    0.10 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | MediumRun  | 15             | 2           | 10          |  2,625.06 ns | 50.400 ns | 70.654 ns |  2,571.04 ns |  26.53 |    0.81 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | MediumRun  | 15             | 2           | 10          |  4,387.70 ns | 30.695 ns | 44.993 ns |  4,383.55 ns |  44.34 |    0.80 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | MediumRun  | 15             | 2           | 10          |  4,313.46 ns | 12.864 ns | 19.255 ns |  4,309.19 ns |  43.59 |    0.68 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | MediumRun  | 15             | 2           | 10          |    504.58 ns |  1.205 ns |  1.689 ns |    505.07 ns |   5.10 |    0.08 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | MediumRun  | 15             | 2           | 10          |     98.97 ns |  1.014 ns |  1.518 ns |     98.63 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | MediumRun  | 15             | 2           | 10          | 10,531.46 ns | 27.635 ns | 40.507 ns | 10,534.77 ns | 106.43 |    1.65 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | MediumRun  | 15             | 2           | 10          |  4,394.55 ns |  8.627 ns | 12.372 ns |  4,393.07 ns |  44.41 |    0.68 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | MediumRun  | 15             | 2           | 10          |    420.80 ns |  2.046 ns |  2.999 ns |    420.77 ns |   4.25 |    0.07 | 0.0310 |     520 B |        2.32 |
