```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   417.65 ns |   8.144 ns |  5.387 ns |   4.35 |    0.09 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   540.17 ns |   2.895 ns |  1.915 ns |   5.63 |    0.10 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 2,476.16 ns |  25.025 ns | 14.892 ns |  25.81 |    0.49 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 3,891.94 ns |  26.581 ns | 15.818 ns |  40.57 |    0.75 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,893.83 ns |  22.620 ns | 13.461 ns |  40.59 |    0.74 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   522.80 ns |  12.321 ns |  7.332 ns |   5.45 |    0.12 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    95.95 ns |   2.712 ns |  1.794 ns |   1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 9,513.98 ns |  20.796 ns | 12.375 ns |  99.19 |    1.79 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 4,025.61 ns |   9.815 ns |  5.841 ns |  41.97 |    0.76 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   423.26 ns |  12.143 ns |  8.032 ns |   4.41 |    0.11 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   413.10 ns |  68.862 ns |  3.775 ns |   4.59 |    0.06 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   525.44 ns |  43.395 ns |  2.379 ns |   5.84 |    0.06 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 2,451.39 ns | 139.192 ns |  7.630 ns |  27.24 |    0.28 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,957.33 ns | 146.606 ns |  8.036 ns |  43.98 |    0.44 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 3,888.36 ns | 115.865 ns |  6.351 ns |  43.21 |    0.43 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   515.45 ns |  21.845 ns |  1.197 ns |   5.73 |    0.06 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    89.99 ns |  18.621 ns |  1.021 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 9,379.73 ns | 297.011 ns | 16.280 ns | 104.24 |    1.04 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 4,108.66 ns | 172.090 ns |  9.433 ns |  45.66 |    0.46 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   463.79 ns | 126.201 ns |  6.917 ns |   5.15 |    0.08 | 0.0310 |     520 B |        2.32 |
