```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    311.4 ns |     1.18 ns |   0.70 ns |  1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    766.3 ns |     5.70 ns |   3.77 ns |  2.46 |    0.01 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,672.8 ns |    17.74 ns |  10.56 ns |  5.37 |    0.03 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    677.5 ns |     3.39 ns |   1.77 ns |  2.18 |    0.01 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,788.6 ns |    65.11 ns |  43.06 ns | 12.17 |    0.13 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,275.8 ns |    12.39 ns |   8.20 ns |  4.10 |    0.03 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,208.8 ns |    15.03 ns |   7.86 ns | 10.30 |    0.03 | 0.1030 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |  9,451.7 ns |   201.52 ns | 133.29 ns | 30.35 |    0.41 | 0.3815 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 16,909.0 ns |   118.96 ns |  70.79 ns | 54.30 |    0.25 | 1.0681 |      - |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    311.0 ns |    38.70 ns |   2.12 ns |  1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    774.4 ns |    18.31 ns |   1.00 ns |  2.49 |    0.01 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,670.9 ns |   201.49 ns |  11.04 ns |  5.37 |    0.04 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    707.0 ns |   116.92 ns |   6.41 ns |  2.27 |    0.02 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,756.6 ns |   191.79 ns |  10.51 ns | 12.08 |    0.08 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,241.8 ns |     1.95 ns |   0.11 ns |  3.99 |    0.02 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,081.9 ns |   181.97 ns |   9.97 ns |  9.91 |    0.06 | 0.1030 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |  9,264.6 ns |   585.09 ns |  32.07 ns | 29.79 |    0.20 | 0.3815 |      - |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 16,639.0 ns | 2,266.21 ns | 124.22 ns | 53.51 |    0.47 | 1.0681 |      - |   18312 B |      163.50 |
