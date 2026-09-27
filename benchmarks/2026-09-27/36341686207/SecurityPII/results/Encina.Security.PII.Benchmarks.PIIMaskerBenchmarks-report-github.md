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
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |    413.32 ns |   1.313 ns |  0.869 ns |   4.05 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |    586.99 ns |   2.340 ns |  1.392 ns |   5.74 |    0.04 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     |  2,683.26 ns |   9.527 ns |  6.301 ns |  26.26 |    0.16 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     |  4,512.05 ns |   6.582 ns |  3.443 ns |  44.16 |    0.26 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     |  4,439.67 ns |  97.863 ns | 64.730 ns |  43.45 |    0.66 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |    507.60 ns |   1.068 ns |  0.636 ns |   4.97 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    102.18 ns |   1.050 ns |  0.625 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 10,415.70 ns |  20.409 ns | 10.674 ns | 101.94 |    0.60 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     |  4,555.02 ns |  20.506 ns | 13.564 ns |  44.58 |    0.29 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |    424.57 ns |   2.298 ns |  1.202 ns |   4.16 |    0.03 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |              |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |    415.02 ns |   7.404 ns |  0.406 ns |   4.18 |    0.02 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |    587.83 ns |  15.728 ns |  0.862 ns |   5.92 |    0.03 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           |  2,746.99 ns | 128.190 ns |  7.027 ns |  27.65 |    0.15 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           |  4,561.24 ns | 236.122 ns | 12.943 ns |  45.91 |    0.25 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           |  4,341.49 ns | 283.661 ns | 15.548 ns |  43.70 |    0.25 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |    523.56 ns |  14.035 ns |  0.769 ns |   5.27 |    0.03 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |     99.35 ns |  10.202 ns |  0.559 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 11,064.91 ns | 427.482 ns | 23.432 ns | 111.38 |    0.58 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           |  4,481.33 ns | 519.461 ns | 28.473 ns |  45.11 |    0.33 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |    422.31 ns |  39.333 ns |  2.156 ns |   4.25 |    0.03 | 0.0310 |     520 B |        2.32 |
