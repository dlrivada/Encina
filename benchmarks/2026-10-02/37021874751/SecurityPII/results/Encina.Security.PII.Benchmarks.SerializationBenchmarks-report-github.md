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
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    438.9 ns |     1.15 ns |  0.68 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,046.0 ns |     3.09 ns |  1.84 ns |  2.38 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,287.3 ns |    43.77 ns | 28.95 ns |  5.21 |    0.06 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    936.1 ns |     1.90 ns |  1.26 ns |  2.13 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,152.2 ns |    52.45 ns | 34.69 ns | 11.74 |    0.08 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,841.9 ns |     4.31 ns |  2.85 ns |  4.20 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,557.0 ns |     9.89 ns |  6.54 ns | 10.38 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,637.3 ns |    45.69 ns | 30.22 ns | 31.07 |    0.08 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 25,552.2 ns |    97.42 ns | 64.44 ns | 58.22 |    0.16 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    440.9 ns |    21.09 ns |  1.16 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,038.6 ns |    50.52 ns |  2.77 ns |  2.36 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,290.9 ns |   282.09 ns | 15.46 ns |  5.20 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    950.7 ns |    18.50 ns |  1.01 ns |  2.16 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,136.2 ns |   891.46 ns | 48.86 ns | 11.65 |    0.10 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,830.7 ns |    82.40 ns |  4.52 ns |  4.15 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,498.3 ns |   626.51 ns | 34.34 ns | 10.20 |    0.07 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,731.4 ns |   493.71 ns | 27.06 ns | 31.15 |    0.09 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 25,061.1 ns | 1,159.74 ns | 63.57 ns | 56.85 |    0.18 | 1.0681 |   18312 B |      163.50 |
