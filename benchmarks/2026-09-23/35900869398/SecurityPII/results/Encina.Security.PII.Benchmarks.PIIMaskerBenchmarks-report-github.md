```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   431.41 ns |  12.792 ns |  8.461 ns |   4.70 |    0.13 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   550.94 ns |   1.884 ns |  0.985 ns |   6.00 |    0.12 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 2,493.03 ns |   4.279 ns |  2.546 ns |  27.16 |    0.55 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 4,050.23 ns |  23.706 ns | 15.680 ns |  44.13 |    0.90 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,890.30 ns |  28.438 ns | 16.923 ns |  42.39 |    0.87 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   513.88 ns |   3.719 ns |  2.213 ns |   5.60 |    0.11 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    91.81 ns |   2.967 ns |  1.963 ns |   1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 9,362.05 ns |  32.596 ns | 19.397 ns | 102.01 |    2.06 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 4,097.60 ns |  15.253 ns | 10.089 ns |  44.65 |    0.90 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   432.88 ns |   9.280 ns |  6.138 ns |   4.72 |    0.11 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   424.52 ns |  84.353 ns |  4.624 ns |   4.54 |    0.11 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   540.54 ns |  62.288 ns |  3.414 ns |   5.78 |    0.13 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 2,490.04 ns | 136.239 ns |  7.468 ns |  26.63 |    0.57 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 4,018.12 ns | 233.485 ns | 12.798 ns |  42.96 |    0.92 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,965.11 ns |  68.896 ns |  3.776 ns |  42.40 |    0.90 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   522.91 ns |  55.581 ns |  3.047 ns |   5.59 |    0.12 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    93.56 ns |  42.278 ns |  2.317 ns |   1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 9,425.75 ns | 262.720 ns | 14.401 ns | 100.79 |    2.14 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 4,023.10 ns | 282.416 ns | 15.480 ns |  43.02 |    0.92 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   431.82 ns |  54.273 ns |  2.975 ns |   4.62 |    0.10 | 0.0310 |     520 B |        2.32 |
