```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    313.1 ns |     0.47 ns |  0.28 ns |  1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    756.3 ns |     4.31 ns |  2.25 ns |  2.42 |    0.01 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,683.8 ns |     6.80 ns |  4.49 ns |  5.38 |    0.01 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    669.9 ns |     2.07 ns |  1.37 ns |  2.14 |    0.00 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,847.6 ns |    68.11 ns | 45.05 ns | 12.29 |    0.14 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,295.7 ns |    12.27 ns |  8.11 ns |  4.14 |    0.02 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,157.3 ns |    34.28 ns | 17.93 ns | 10.08 |    0.05 | 0.1030 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |  9,454.4 ns |   101.16 ns | 66.91 ns | 30.20 |    0.21 | 0.3815 |      - |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 17,111.4 ns |    91.85 ns | 60.75 ns | 54.65 |    0.19 | 1.0681 |      - |   18312 B |      163.50 |
|                          |            |                |             |             |             |          |       |         |        |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    318.6 ns |   127.18 ns |  6.97 ns |  1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    764.9 ns |    46.63 ns |  2.56 ns |  2.40 |    0.05 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,686.0 ns | 1,162.46 ns | 63.72 ns |  5.29 |    0.20 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    727.8 ns |   266.14 ns | 14.59 ns |  2.29 |    0.06 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,953.9 ns |   371.55 ns | 20.37 ns | 12.42 |    0.24 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,251.7 ns |   327.29 ns | 17.94 ns |  3.93 |    0.09 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,198.9 ns |   161.11 ns |  8.83 ns | 10.05 |    0.19 | 0.1030 |      - |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |  9,613.8 ns | 1,693.68 ns | 92.84 ns | 30.19 |    0.62 | 0.3815 |      - |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 16,837.7 ns |   733.21 ns | 40.19 ns | 52.87 |    1.00 | 1.0681 |      - |   18312 B |      163.50 |
