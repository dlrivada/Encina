```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    416.6 ns |   7.85 ns |  5.19 ns |  1.00 |    0.02 | 0.0043 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    968.9 ns |   7.81 ns |  5.17 ns |  2.33 |    0.03 | 0.0153 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,242.1 ns |  13.90 ns |  9.20 ns |  5.38 |    0.07 | 0.1488 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    898.0 ns |   1.25 ns |  0.65 ns |  2.16 |    0.03 | 0.0086 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,364.4 ns |  16.10 ns |  8.42 ns | 12.88 |    0.15 | 0.3052 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,716.6 ns |   4.43 ns |  2.32 ns |  4.12 |    0.05 | 0.0324 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,864.6 ns |  17.75 ns | 11.74 ns | 11.68 |    0.14 | 0.0687 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,762.3 ns |  63.69 ns | 42.12 ns | 33.04 |    0.40 | 0.2441 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 25,183.1 ns |  52.56 ns | 31.28 ns | 60.45 |    0.72 | 0.7019 |   18312 B |      163.50 |
|                          |            |                |             |             |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    407.0 ns |  21.63 ns |  1.19 ns |  1.00 |    0.00 | 0.0043 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    975.8 ns |  96.60 ns |  5.30 ns |  2.40 |    0.01 | 0.0153 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,213.7 ns |  63.21 ns |  3.46 ns |  5.44 |    0.02 | 0.1488 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    899.8 ns |  22.36 ns |  1.23 ns |  2.21 |    0.01 | 0.0086 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,372.0 ns | 448.88 ns | 24.60 ns | 13.20 |    0.06 | 0.3052 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,725.5 ns |  80.77 ns |  4.43 ns |  4.24 |    0.01 | 0.0324 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  5,245.0 ns | 149.11 ns |  8.17 ns | 12.89 |    0.04 | 0.0687 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,829.1 ns | 250.98 ns | 13.76 ns | 33.98 |    0.09 | 0.2441 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 25,901.2 ns | 612.44 ns | 33.57 ns | 63.63 |    0.18 | 0.7019 |   18312 B |      163.50 |
