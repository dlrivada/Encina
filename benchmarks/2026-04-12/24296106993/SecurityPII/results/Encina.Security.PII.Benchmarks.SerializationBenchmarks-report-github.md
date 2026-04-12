```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |    438.5 ns |   0.89 ns |   0.53 ns |    438.5 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           |  1,022.4 ns |   1.84 ns |   1.10 ns |  1,022.6 ns |  2.33 |    0.00 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           |  2,181.8 ns |  40.72 ns |  26.94 ns |  2,174.0 ns |  4.98 |    0.06 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |    936.2 ns |   1.89 ns |   1.25 ns |    936.1 ns |  2.14 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           |  4,896.2 ns |  31.03 ns |  20.52 ns |  4,902.6 ns | 11.17 |    0.05 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           |  1,819.8 ns |   5.84 ns |   3.48 ns |  1,820.5 ns |  4.15 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           |  4,518.6 ns |  15.74 ns |  10.41 ns |  4,516.7 ns | 10.31 |    0.03 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           | 13,633.7 ns | 101.84 ns |  60.60 ns | 13,641.2 ns | 31.09 |    0.14 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           | 24,171.1 ns | 138.04 ns |  91.31 ns | 24,192.2 ns | 55.13 |    0.21 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |           |             |       |         |        |           |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |    452.9 ns |   4.73 ns |   6.79 ns |    457.6 ns |  1.00 |    0.02 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          |  1,017.6 ns |   3.29 ns |   4.72 ns |  1,017.2 ns |  2.25 |    0.03 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          |  2,174.0 ns |  17.29 ns |  25.88 ns |  2,173.5 ns |  4.80 |    0.09 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |    934.2 ns |   1.55 ns |   2.23 ns |    934.1 ns |  2.06 |    0.03 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          |  4,783.2 ns |  13.05 ns |  18.29 ns |  4,787.5 ns | 10.56 |    0.16 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          |  1,821.5 ns |   2.89 ns |   4.14 ns |  1,821.4 ns |  4.02 |    0.06 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          |  4,665.4 ns |  85.14 ns | 119.35 ns |  4,762.7 ns | 10.30 |    0.30 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          | 13,524.7 ns | 121.40 ns | 174.10 ns | 13,526.2 ns | 29.87 |    0.58 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          | 23,627.0 ns | 106.98 ns | 153.43 ns | 23,722.6 ns | 52.18 |    0.84 | 1.0681 |   18312 B |      163.50 |
