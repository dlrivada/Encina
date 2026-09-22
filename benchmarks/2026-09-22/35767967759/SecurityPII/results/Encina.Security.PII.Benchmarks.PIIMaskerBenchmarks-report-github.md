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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    396.85 ns |   8.445 ns |  5.586 ns |   4.20 |    0.11 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    602.28 ns |   6.318 ns |  4.179 ns |   6.37 |    0.15 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,748.91 ns |  16.224 ns | 10.731 ns |  29.07 |    0.67 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,466.98 ns |  32.035 ns | 19.064 ns |  47.24 |    1.09 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,381.71 ns |  34.947 ns | 20.796 ns |  46.34 |    1.07 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    501.93 ns |   8.202 ns |  5.425 ns |   5.31 |    0.13 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |     94.60 ns |   3.420 ns |  2.262 ns |   1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,416.71 ns |  99.489 ns | 65.806 ns | 110.17 |    2.59 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,626.23 ns |  50.689 ns | 30.164 ns |  48.93 |    1.15 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    414.03 ns |   9.180 ns |  6.072 ns |   4.38 |    0.12 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    410.09 ns |  85.593 ns |  4.692 ns |   4.38 |    0.07 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    587.73 ns |  11.791 ns |  0.646 ns |   6.28 |    0.07 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,733.34 ns | 221.926 ns | 12.165 ns |  29.20 |    0.36 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,457.74 ns | 375.288 ns | 20.571 ns |  47.62 |    0.59 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,454.34 ns | 843.245 ns | 46.221 ns |  47.58 |    0.70 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    507.10 ns |  46.360 ns |  2.541 ns |   5.42 |    0.07 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     93.63 ns |  23.374 ns |  1.281 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 10,618.78 ns | 957.389 ns | 52.478 ns | 113.43 |    1.42 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,517.45 ns | 139.329 ns |  7.637 ns |  48.26 |    0.57 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    432.42 ns |  20.663 ns |  1.133 ns |   4.62 |    0.06 | 0.0310 |     520 B |        2.32 |
