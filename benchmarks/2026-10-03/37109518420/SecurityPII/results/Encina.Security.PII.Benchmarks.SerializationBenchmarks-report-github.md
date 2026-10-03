```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    528.5 ns |   1.78 ns |  1.06 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,124.9 ns |   1.92 ns |  1.14 ns |  2.13 |    0.00 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,436.7 ns |  33.02 ns | 21.84 ns |  4.61 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |  1,000.1 ns |   3.68 ns |  2.44 ns |  1.89 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,348.2 ns |  17.22 ns | 10.25 ns | 10.12 |    0.03 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,879.3 ns |   6.17 ns |  3.67 ns |  3.56 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,572.5 ns |  11.72 ns |  6.97 ns |  8.65 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,783.3 ns |  48.83 ns | 29.06 ns | 26.08 |    0.07 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,835.7 ns |  91.43 ns | 60.47 ns | 46.99 |    0.14 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    485.0 ns |  37.59 ns |  2.06 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,120.7 ns |  54.69 ns |  3.00 ns |  2.31 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,428.3 ns | 391.38 ns | 21.45 ns |  5.01 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    939.1 ns |  37.24 ns |  2.04 ns |  1.94 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,406.3 ns | 281.69 ns | 15.44 ns | 11.15 |    0.05 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,864.8 ns |  94.77 ns |  5.19 ns |  3.84 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,652.5 ns | 338.57 ns | 18.56 ns |  9.59 |    0.05 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 14,470.6 ns | 469.96 ns | 25.76 ns | 29.83 |    0.12 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 25,193.2 ns | 693.88 ns | 38.03 ns | 51.94 |    0.20 | 1.0681 |   18312 B |      163.50 |
