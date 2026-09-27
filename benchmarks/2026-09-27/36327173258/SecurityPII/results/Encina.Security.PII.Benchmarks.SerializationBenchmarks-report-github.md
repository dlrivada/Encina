```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    322.4 ns |     0.79 ns |   0.47 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    754.1 ns |     1.58 ns |   1.05 ns |  2.34 |    0.00 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,636.9 ns |    26.19 ns |  15.59 ns |  5.08 |    0.05 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    671.6 ns |     0.76 ns |   0.45 ns |  2.08 |    0.00 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,748.7 ns |    40.92 ns |  27.07 ns | 11.63 |    0.08 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,204.6 ns |    10.02 ns |   6.62 ns |  3.74 |    0.02 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,122.5 ns |    47.33 ns |  31.30 ns |  9.69 |    0.09 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 10,452.2 ns |   183.90 ns |  96.18 ns | 32.42 |    0.28 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 19,107.8 ns |   107.38 ns |  71.02 ns | 59.27 |    0.23 | 0.2136 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    322.4 ns |    11.85 ns |   0.65 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    755.8 ns |   214.20 ns |  11.74 ns |  2.34 |    0.03 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,582.7 ns |    37.01 ns |   2.03 ns |  4.91 |    0.01 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    666.4 ns |    79.01 ns |   4.33 ns |  2.07 |    0.01 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,773.5 ns |   181.84 ns |   9.97 ns | 11.70 |    0.03 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,185.7 ns |   109.31 ns |   5.99 ns |  3.68 |    0.02 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,135.6 ns |   226.72 ns |  12.43 ns |  9.73 |    0.04 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 10,709.0 ns | 5,154.00 ns | 282.51 ns | 33.22 |    0.76 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 18,866.5 ns | 1,745.01 ns |  95.65 ns | 58.52 |    0.28 | 0.2136 |   18312 B |      163.50 |
