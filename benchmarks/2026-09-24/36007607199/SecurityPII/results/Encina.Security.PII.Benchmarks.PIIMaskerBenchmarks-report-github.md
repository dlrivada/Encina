```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    423.15 ns |   1.659 ns |  0.987 ns |   4.44 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    599.39 ns |   4.749 ns |  3.141 ns |   6.28 |    0.07 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,784.08 ns |  15.496 ns |  9.221 ns |  29.18 |    0.30 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,414.59 ns |  11.063 ns |  6.583 ns |  46.27 |    0.45 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,384.91 ns |  10.948 ns |  6.515 ns |  45.96 |    0.45 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    508.89 ns |  10.447 ns |  6.217 ns |   5.33 |    0.08 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     95.42 ns |   1.465 ns |  0.969 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,575.69 ns | 138.068 ns | 91.323 ns | 110.85 |    1.41 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,582.35 ns |  10.505 ns |  6.948 ns |  48.03 |    0.47 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    424.57 ns |   7.840 ns |  5.185 ns |   4.45 |    0.07 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    416.99 ns |  54.239 ns |  2.973 ns |   3.98 |    0.03 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    555.56 ns |  65.941 ns |  3.614 ns |   5.30 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,616.91 ns | 152.651 ns |  8.367 ns |  24.98 |    0.11 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,377.44 ns |  68.617 ns |  3.761 ns |  41.79 |    0.15 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,427.72 ns |  43.429 ns |  2.380 ns |  42.27 |    0.15 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    509.32 ns |  33.725 ns |  1.849 ns |   4.86 |    0.02 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    104.76 ns |   7.653 ns |  0.419 ns |   1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,989.51 ns | 811.434 ns | 44.477 ns | 104.91 |    0.52 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,551.04 ns | 464.717 ns | 25.473 ns |  43.44 |    0.26 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    427.17 ns |  20.741 ns |  1.137 ns |   4.08 |    0.02 | 0.0310 |     520 B |        2.32 |
