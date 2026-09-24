```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    319.8 ns |   1.17 ns |  0.61 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    757.1 ns |   3.79 ns |  2.51 ns |  2.37 |    0.01 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,572.3 ns |  20.25 ns | 13.39 ns |  4.92 |    0.04 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    664.6 ns |   1.47 ns |  0.87 ns |  2.08 |    0.00 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,815.8 ns |  97.22 ns | 57.85 ns | 11.93 |    0.17 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,239.3 ns |   9.31 ns |  4.87 ns |  3.88 |    0.02 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,134.0 ns |  10.77 ns |  7.12 ns |  9.80 |    0.03 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 10,620.0 ns |  98.68 ns | 58.73 ns | 33.21 |    0.18 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 19,673.4 ns | 114.19 ns | 75.53 ns | 61.53 |    0.25 | 0.2136 |   18312 B |      163.50 |
|                          |            |                |             |             |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    323.7 ns |   3.95 ns |  0.22 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    752.7 ns |  24.09 ns |  1.32 ns |  2.33 |    0.00 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,650.9 ns | 257.88 ns | 14.14 ns |  5.10 |    0.04 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    667.1 ns |  21.47 ns |  1.18 ns |  2.06 |    0.00 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,917.7 ns | 794.40 ns | 43.54 ns | 12.10 |    0.12 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,223.5 ns |  39.04 ns |  2.14 ns |  3.78 |    0.01 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,173.6 ns | 100.44 ns |  5.51 ns |  9.80 |    0.02 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 10,663.1 ns | 322.69 ns | 17.69 ns | 32.94 |    0.05 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 19,724.8 ns | 909.33 ns | 49.84 ns | 60.93 |    0.14 | 0.2136 |   18312 B |      163.50 |
