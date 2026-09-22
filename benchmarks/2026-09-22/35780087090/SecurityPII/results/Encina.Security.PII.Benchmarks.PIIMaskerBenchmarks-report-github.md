```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   372.94 ns |    11.049 ns |   7.308 ns |  4.32 |    0.13 | 0.0062 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   489.07 ns |     9.019 ns |   5.367 ns |  5.67 |    0.15 | 0.0048 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 1,944.01 ns |    32.584 ns |  21.552 ns | 22.53 |    0.58 | 0.0114 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,275.53 ns |    68.330 ns |  45.196 ns | 37.96 |    1.02 | 0.0191 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,287.00 ns |    78.327 ns |  51.808 ns | 38.09 |    1.06 | 0.0191 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   461.47 ns |     9.907 ns |   5.896 ns |  5.35 |    0.14 | 0.0062 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    86.35 ns |     3.235 ns |   2.140 ns |  1.00 |    0.03 | 0.0026 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 8,493.18 ns |   100.505 ns |  59.809 ns | 98.41 |    2.41 | 0.0458 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 3,312.82 ns |    44.719 ns |  23.389 ns | 38.39 |    0.94 | 0.0191 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   377.11 ns |    11.522 ns |   6.857 ns |  4.37 |    0.13 | 0.0062 |     520 B |        2.32 |
|                          |            |                |             |             |              |            |       |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   366.03 ns |    84.719 ns |   4.644 ns |  4.06 |    0.05 | 0.0062 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   498.55 ns |   130.183 ns |   7.136 ns |  5.53 |    0.08 | 0.0048 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 1,965.27 ns | 1,443.888 ns |  79.144 ns | 21.82 |    0.78 | 0.0114 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,319.33 ns | 1,355.136 ns |  74.280 ns | 36.85 |    0.77 | 0.0191 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,265.01 ns |   718.767 ns |  39.398 ns | 36.24 |    0.47 | 0.0191 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   452.68 ns |    78.010 ns |   4.276 ns |  5.02 |    0.06 | 0.0062 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    90.09 ns |    14.721 ns |   0.807 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 8,715.43 ns | 4,853.834 ns | 266.055 ns | 96.74 |    2.67 | 0.0458 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 3,279.91 ns |   779.409 ns |  42.722 ns | 36.41 |    0.50 | 0.0191 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   377.47 ns |   112.287 ns |   6.155 ns |  4.19 |    0.07 | 0.0062 |     520 B |        2.32 |
