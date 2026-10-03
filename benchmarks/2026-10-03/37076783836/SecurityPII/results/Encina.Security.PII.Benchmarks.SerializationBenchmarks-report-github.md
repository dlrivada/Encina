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
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    319.7 ns |     0.56 ns |   0.33 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    743.9 ns |     1.61 ns |   0.96 ns |  2.33 |    0.00 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,567.4 ns |    20.65 ns |  13.66 ns |  4.90 |    0.04 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    663.9 ns |     4.01 ns |   2.09 ns |  2.08 |    0.01 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,658.0 ns |    12.57 ns |   7.48 ns | 11.44 |    0.02 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,167.0 ns |     4.12 ns |   2.72 ns |  3.65 |    0.01 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  2,979.7 ns |    34.05 ns |  22.52 ns |  9.32 |    0.07 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |  9,947.8 ns |   223.65 ns | 133.09 ns | 31.12 |    0.40 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 17,817.8 ns |   142.93 ns |  85.06 ns | 55.73 |    0.26 | 0.2136 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    321.2 ns |     8.10 ns |   0.44 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    735.7 ns |    19.71 ns |   1.08 ns |  2.29 |    0.00 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,552.1 ns |    69.58 ns |   3.81 ns |  4.83 |    0.01 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    670.8 ns |    22.83 ns |   1.25 ns |  2.09 |    0.00 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,667.5 ns |   160.60 ns |   8.80 ns | 11.42 |    0.03 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,229.5 ns |   616.91 ns |  33.82 ns |  3.83 |    0.09 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,019.3 ns |   371.71 ns |  20.37 ns |  9.40 |    0.06 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |  9,781.6 ns |   422.31 ns |  23.15 ns | 30.45 |    0.07 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 17,870.5 ns | 1,067.99 ns |  58.54 ns | 55.64 |    0.17 | 0.2136 |   18312 B |      163.50 |
