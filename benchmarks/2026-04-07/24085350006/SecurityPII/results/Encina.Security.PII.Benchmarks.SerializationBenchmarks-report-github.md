```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    424.4 ns |     7.92 ns |   5.24 ns |  1.00 |    0.02 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,001.0 ns |    12.02 ns |   7.95 ns |  2.36 |    0.03 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,105.1 ns |    51.12 ns |  33.81 ns |  4.96 |    0.10 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    886.9 ns |     8.79 ns |   5.81 ns |  2.09 |    0.03 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  4,749.6 ns |   100.66 ns |  66.58 ns | 11.19 |    0.20 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,803.5 ns |    28.85 ns |  19.08 ns |  4.25 |    0.07 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,490.9 ns |    70.98 ns |  46.95 ns | 10.58 |    0.16 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,359.8 ns |   168.17 ns | 111.24 ns | 31.48 |    0.44 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,113.9 ns |   361.93 ns | 239.40 ns | 56.82 |    0.85 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    432.9 ns |    55.28 ns |   3.03 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,019.4 ns |    87.58 ns |   4.80 ns |  2.35 |    0.02 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,099.3 ns |   183.31 ns |  10.05 ns |  4.85 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    902.8 ns |   113.70 ns |   6.23 ns |  2.09 |    0.02 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  4,879.7 ns | 1,301.68 ns |  71.35 ns | 11.27 |    0.16 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,798.4 ns |   228.71 ns |  12.54 ns |  4.15 |    0.04 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,360.9 ns |   242.16 ns |  13.27 ns | 10.07 |    0.07 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 12,969.6 ns | 4,138.63 ns | 226.85 ns | 29.96 |    0.49 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 24,351.6 ns | 2,209.00 ns | 121.08 ns | 56.26 |    0.42 | 1.0681 |   18312 B |      163.50 |
