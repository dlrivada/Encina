```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    224.5 ns |     3.77 ns |   2.49 ns |  1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    527.5 ns |    22.15 ns |  14.65 ns |  2.35 |    0.07 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,200.3 ns |    16.78 ns |   9.98 ns |  5.35 |    0.07 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    518.7 ns |    12.59 ns |   7.49 ns |  2.31 |    0.04 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  2,550.1 ns |   112.49 ns |  66.94 ns | 11.36 |    0.31 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |    951.0 ns |    27.70 ns |  16.48 ns |  4.24 |    0.08 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  2,296.2 ns |    58.69 ns |  38.82 ns | 10.23 |    0.20 | 0.1030 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |  6,970.0 ns |   246.28 ns | 146.56 ns | 31.05 |    0.70 | 0.3815 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 12,432.2 ns |   537.19 ns | 319.67 ns | 55.37 |    1.47 | 1.0834 | 0.0153 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    237.6 ns |   236.77 ns |  12.98 ns |  1.00 |    0.07 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    563.4 ns |   118.48 ns |   6.49 ns |  2.38 |    0.11 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,043.4 ns |    25.55 ns |   1.40 ns |  4.40 |    0.20 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    513.8 ns |   175.33 ns |   9.61 ns |  2.17 |    0.11 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  2,561.4 ns |   376.73 ns |  20.65 ns | 10.80 |    0.50 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |    913.4 ns |   169.88 ns |   9.31 ns |  3.85 |    0.18 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  2,184.6 ns |   523.38 ns |  28.69 ns |  9.21 |    0.44 | 0.1030 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |  6,686.5 ns | 2,127.48 ns | 116.61 ns | 28.19 |    1.37 | 0.3815 |      - |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 11,595.4 ns | 6,120.14 ns | 335.47 ns | 48.89 |    2.56 | 1.0834 | 0.0153 |   18312 B |      163.50 |
