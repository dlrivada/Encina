```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    441.5 ns |     2.19 ns |  1.45 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,024.9 ns |     1.56 ns |  1.03 ns |  2.32 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,268.0 ns |    26.78 ns | 17.71 ns |  5.14 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    906.0 ns |     1.50 ns |  0.99 ns |  2.05 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,104.0 ns |    39.85 ns | 26.36 ns | 11.56 |    0.07 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,796.7 ns |     6.93 ns |  4.13 ns |  4.07 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,585.8 ns |    12.14 ns |  8.03 ns | 10.39 |    0.04 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,544.1 ns |    43.40 ns | 25.82 ns | 30.67 |    0.11 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,507.7 ns |   159.71 ns | 95.04 ns | 55.50 |    0.27 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    440.4 ns |    22.56 ns |  1.24 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,044.7 ns |    39.33 ns |  2.16 ns |  2.37 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,248.2 ns |    25.11 ns |  1.38 ns |  5.10 |    0.01 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    908.0 ns |    53.43 ns |  2.93 ns |  2.06 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,136.8 ns |   267.95 ns | 14.69 ns | 11.66 |    0.04 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,770.4 ns |    63.77 ns |  3.50 ns |  4.02 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,477.4 ns |    32.94 ns |  1.81 ns | 10.17 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,848.9 ns |   116.67 ns |  6.40 ns | 31.45 |    0.08 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 24,359.7 ns | 1,122.81 ns | 61.55 ns | 55.31 |    0.18 | 1.0681 |   18312 B |      163.50 |
