```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    441.2 ns |     0.58 ns |  0.34 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    969.9 ns |     1.99 ns |  1.32 ns |  2.20 |    0.00 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,238.6 ns |    24.89 ns | 14.81 ns |  5.07 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    912.9 ns |    11.88 ns |  7.07 ns |  2.07 |    0.02 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,049.5 ns |    51.31 ns | 30.54 ns | 11.45 |    0.07 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,610.3 ns |     5.45 ns |  3.60 ns |  3.65 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,109.0 ns |    10.42 ns |  6.89 ns |  9.31 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 12,170.2 ns |    47.27 ns | 24.72 ns | 27.59 |    0.06 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 21,812.4 ns |   128.91 ns | 85.27 ns | 49.44 |    0.19 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    403.4 ns |    15.82 ns |  0.87 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    976.7 ns |    60.02 ns |  3.29 ns |  2.42 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,200.3 ns |   429.68 ns | 23.55 ns |  5.45 |    0.05 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    906.7 ns |   307.27 ns | 16.84 ns |  2.25 |    0.04 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  4,974.8 ns |   167.62 ns |  9.19 ns | 12.33 |    0.03 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,602.5 ns |     8.24 ns |  0.45 ns |  3.97 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,056.6 ns |   263.45 ns | 14.44 ns | 10.06 |    0.04 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 12,330.1 ns |   391.65 ns | 21.47 ns | 30.57 |    0.07 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 22,306.5 ns | 1,272.87 ns | 69.77 ns | 55.30 |    0.18 | 1.0681 |   18312 B |      163.50 |
