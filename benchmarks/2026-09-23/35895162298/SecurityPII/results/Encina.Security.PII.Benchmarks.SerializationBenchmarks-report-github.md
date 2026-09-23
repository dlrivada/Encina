```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    445.7 ns |   0.96 ns |  0.64 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,055.5 ns |   3.51 ns |  2.09 ns |  2.37 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,281.6 ns |  28.80 ns | 19.05 ns |  5.12 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    932.3 ns |   0.73 ns |  0.38 ns |  2.09 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,135.0 ns |  58.09 ns | 38.42 ns | 11.52 |    0.08 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,787.0 ns |   4.82 ns |  3.19 ns |  4.01 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,529.5 ns |  14.34 ns |  9.49 ns | 10.16 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,530.5 ns |  68.93 ns | 45.59 ns | 30.36 |    0.11 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,329.7 ns | 110.98 ns | 66.04 ns | 54.58 |    0.16 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    431.2 ns |  17.89 ns |  0.98 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,059.2 ns |  36.60 ns |  2.01 ns |  2.46 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,283.6 ns | 456.41 ns | 25.02 ns |  5.30 |    0.05 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    933.0 ns |  52.83 ns |  2.90 ns |  2.16 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  4,989.0 ns | 479.64 ns | 26.29 ns | 11.57 |    0.06 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,806.1 ns |  50.25 ns |  2.75 ns |  4.19 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,821.7 ns | 159.25 ns |  8.73 ns | 11.18 |    0.03 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,751.8 ns | 602.02 ns | 33.00 ns | 31.89 |    0.09 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 24,245.9 ns | 427.83 ns | 23.45 ns | 56.23 |    0.12 | 1.0681 |   18312 B |      163.50 |
