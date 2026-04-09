```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.50GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |    428.6 ns |   3.27 ns |   2.16 ns |  1.00 |    0.01 | 0.0043 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           |    985.8 ns |   6.10 ns |   4.04 ns |  2.30 |    0.01 | 0.0153 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           |  2,211.7 ns |   8.61 ns |   5.12 ns |  5.16 |    0.03 | 0.1488 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |    938.4 ns |   3.07 ns |   1.83 ns |  2.19 |    0.01 | 0.0086 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           |  5,266.4 ns |  49.78 ns |  32.92 ns | 12.29 |    0.09 | 0.3052 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           |  1,847.0 ns |  12.75 ns |   8.43 ns |  4.31 |    0.03 | 0.0324 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           |  4,744.3 ns |  17.64 ns |  11.66 ns | 11.07 |    0.06 | 0.0687 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           | 14,130.5 ns |  38.21 ns |  25.27 ns | 32.97 |    0.17 | 0.2441 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           | 24,917.0 ns | 100.13 ns |  59.58 ns | 58.13 |    0.31 | 0.7019 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |           |       |         |        |           |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |    439.7 ns |   3.81 ns |   5.70 ns |  1.00 |    0.02 | 0.0043 |     112 B |        1.00 |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          |  1,044.0 ns |   9.63 ns |  14.42 ns |  2.37 |    0.04 | 0.0153 |     392 B |        3.50 |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          |  2,243.1 ns |  18.97 ns |  27.81 ns |  5.10 |    0.09 | 0.1488 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |    938.1 ns |   2.71 ns |   3.80 ns |  2.13 |    0.03 | 0.0086 |     232 B |        2.07 |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          |  5,233.4 ns |  11.85 ns |  17.37 ns | 11.90 |    0.16 | 0.3052 |    7688 B |       68.64 |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          |  1,813.3 ns |   4.61 ns |   6.62 ns |  4.12 |    0.05 | 0.0324 |     840 B |        7.50 |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          |  5,021.0 ns |  13.47 ns |  19.31 ns | 11.42 |    0.15 | 0.0687 |    1752 B |       15.64 |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          | 13,873.6 ns |  20.61 ns |  28.89 ns | 31.56 |    0.41 | 0.2441 |    6504 B |       58.07 |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          | 25,312.1 ns | 178.43 ns | 255.90 ns | 57.57 |    0.93 | 0.7019 |   18312 B |      163.50 |
