```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    436.1 ns |     1.33 ns |   0.88 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,059.8 ns |     3.94 ns |   2.61 ns |  2.43 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,223.3 ns |    31.06 ns |  20.55 ns |  5.10 |    0.05 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    929.9 ns |     5.53 ns |   3.66 ns |  2.13 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  4,960.3 ns |    17.29 ns |  11.44 ns | 11.37 |    0.03 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,764.0 ns |     8.06 ns |   5.33 ns |  4.04 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,563.9 ns |    15.39 ns |  10.18 ns | 10.46 |    0.03 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,637.0 ns |    48.21 ns |  31.89 ns | 31.27 |    0.09 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,345.3 ns |   244.18 ns | 161.51 ns | 55.82 |    0.37 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    426.0 ns |     9.87 ns |   0.54 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,023.6 ns |    38.23 ns |   2.10 ns |  2.40 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,182.6 ns |   449.84 ns |  24.66 ns |  5.12 |    0.05 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    912.9 ns |    34.47 ns |   1.89 ns |  2.14 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,029.1 ns | 2,908.38 ns | 159.42 ns | 11.81 |    0.32 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,769.0 ns |    78.10 ns |   4.28 ns |  4.15 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,799.1 ns |   305.07 ns |  16.72 ns | 11.27 |    0.04 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,862.8 ns |   379.63 ns |  20.81 ns | 32.55 |    0.06 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 24,209.5 ns | 1,099.09 ns |  60.24 ns | 56.84 |    0.14 | 1.0681 |   18312 B |      163.50 |
