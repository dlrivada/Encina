```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    424.0 ns |     3.13 ns |  2.07 ns |  1.00 |    0.01 | 0.0043 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    990.2 ns |    11.30 ns |  6.72 ns |  2.34 |    0.02 | 0.0153 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,241.1 ns |    19.60 ns | 12.97 ns |  5.29 |    0.04 | 0.1488 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    932.5 ns |     2.11 ns |  1.26 ns |  2.20 |    0.01 | 0.0086 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,535.4 ns |    64.27 ns | 42.51 ns | 13.05 |    0.11 | 0.3052 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,842.6 ns |     6.72 ns |  4.45 ns |  4.35 |    0.02 | 0.0324 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,877.5 ns |    24.43 ns | 16.16 ns | 11.50 |    0.06 | 0.0687 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,963.0 ns |    55.83 ns | 33.22 ns | 32.93 |    0.17 | 0.2441 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,552.1 ns |    49.46 ns | 32.71 ns | 57.90 |    0.28 | 0.7019 |   18312 B |      163.50 |
|                          |            |                |             |             |             |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    415.3 ns |    27.19 ns |  1.49 ns |  1.00 |    0.00 | 0.0043 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    986.9 ns |   106.80 ns |  5.85 ns |  2.38 |    0.01 | 0.0153 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,227.5 ns |    93.90 ns |  5.15 ns |  5.36 |    0.02 | 0.1488 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    956.0 ns |    29.43 ns |  1.61 ns |  2.30 |    0.01 | 0.0076 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,709.2 ns |    70.31 ns |  3.85 ns | 13.75 |    0.04 | 0.3052 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,725.8 ns |   147.42 ns |  8.08 ns |  4.16 |    0.02 | 0.0324 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,810.2 ns |   165.39 ns |  9.07 ns | 11.58 |    0.04 | 0.0687 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,843.1 ns |   859.35 ns | 47.10 ns | 33.33 |    0.14 | 0.2441 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 24,816.2 ns | 1,095.04 ns | 60.02 ns | 59.75 |    0.22 | 0.7019 |   18312 B |      163.50 |
