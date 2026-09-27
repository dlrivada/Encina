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
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    331.3 ns |     1.56 ns |   0.93 ns |  1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    759.6 ns |     4.62 ns |   3.05 ns |  2.29 |    0.01 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,730.5 ns |    74.50 ns |  49.28 ns |  5.22 |    0.14 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    707.4 ns |     2.28 ns |   1.51 ns |  2.14 |    0.01 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,845.4 ns |    39.49 ns |  23.50 ns | 11.61 |    0.07 | 0.4578 |      - |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,243.9 ns |    14.11 ns |   9.33 ns |  3.75 |    0.03 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,163.5 ns |    17.18 ns |  10.22 ns |  9.55 |    0.04 | 0.1030 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |  9,438.7 ns |    33.48 ns |  22.15 ns | 28.49 |    0.10 | 0.3815 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 17,074.2 ns |   214.72 ns | 142.02 ns | 51.53 |    0.43 | 1.0681 |      - |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    311.3 ns |     4.82 ns |   0.26 ns |  1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    779.1 ns |    31.19 ns |   1.71 ns |  2.50 |    0.01 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,686.5 ns |    87.99 ns |   4.82 ns |  5.42 |    0.01 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    709.4 ns |    97.11 ns |   5.32 ns |  2.28 |    0.01 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,963.8 ns |   462.44 ns |  25.35 ns | 12.73 |    0.07 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,272.9 ns |   135.19 ns |   7.41 ns |  4.09 |    0.02 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,144.4 ns |   317.89 ns |  17.42 ns | 10.10 |    0.05 | 0.1030 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |  9,526.6 ns | 1,603.64 ns |  87.90 ns | 30.60 |    0.25 | 0.3815 |      - |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 17,181.3 ns | 3,712.36 ns | 203.49 ns | 55.20 |    0.57 | 1.0681 |      - |   18312 B |      163.50 |
