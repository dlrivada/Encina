```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    330.0 ns |     1.76 ns |   1.05 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    797.2 ns |     3.63 ns |   1.90 ns |  2.42 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,713.6 ns |    14.36 ns |   8.54 ns |  5.19 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    696.1 ns |     2.07 ns |   1.23 ns |  2.11 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,991.1 ns |   220.77 ns | 146.03 ns | 12.09 |    0.42 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,261.8 ns |     5.99 ns |   3.57 ns |  3.82 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,123.4 ns |    34.01 ns |  20.24 ns |  9.47 |    0.06 | 0.1030 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |  9,515.4 ns |    83.21 ns |  55.04 ns | 28.84 |    0.18 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 17,194.4 ns |   385.52 ns | 255.00 ns | 52.11 |    0.75 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    308.9 ns |    15.42 ns |   0.85 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    796.0 ns |   113.05 ns |   6.20 ns |  2.58 |    0.02 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,741.2 ns | 1,310.69 ns |  71.84 ns |  5.64 |    0.20 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    753.7 ns |    46.38 ns |   2.54 ns |  2.44 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,866.5 ns | 1,936.66 ns | 106.15 ns | 12.52 |    0.30 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,264.8 ns |   142.78 ns |   7.83 ns |  4.09 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,184.6 ns |   268.61 ns |  14.72 ns | 10.31 |    0.05 | 0.1030 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |  9,370.6 ns | 1,145.22 ns |  62.77 ns | 30.34 |    0.19 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 17,034.3 ns | 2,337.01 ns | 128.10 ns | 55.14 |    0.38 | 1.0681 |   18312 B |      163.50 |
