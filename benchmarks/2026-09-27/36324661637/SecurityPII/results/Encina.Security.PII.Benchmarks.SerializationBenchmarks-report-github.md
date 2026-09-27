```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    452.4 ns |     1.23 ns |  0.73 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,021.5 ns |     3.12 ns |  1.85 ns |  2.26 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,230.1 ns |    24.57 ns | 16.25 ns |  4.93 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    927.9 ns |     6.12 ns |  4.05 ns |  2.05 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  4,926.6 ns |    39.13 ns | 25.88 ns | 10.89 |    0.06 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,784.0 ns |     5.70 ns |  3.77 ns |  3.94 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,515.0 ns |    11.97 ns |  7.92 ns |  9.98 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,259.7 ns |    41.53 ns | 21.72 ns | 29.31 |    0.06 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,264.8 ns |    76.21 ns | 50.41 ns | 53.64 |    0.13 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    454.4 ns |    11.33 ns |  0.62 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,011.7 ns |    60.05 ns |  3.29 ns |  2.23 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,271.5 ns |   289.64 ns | 15.88 ns |  5.00 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    931.1 ns |    37.21 ns |  2.04 ns |  2.05 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,013.9 ns |   435.81 ns | 23.89 ns | 11.03 |    0.05 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,764.0 ns |    53.46 ns |  2.93 ns |  3.88 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,601.3 ns |   186.66 ns | 10.23 ns | 10.13 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,641.0 ns |   466.15 ns | 25.55 ns | 30.02 |    0.06 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 24,132.5 ns | 1,313.96 ns | 72.02 ns | 53.10 |    0.15 | 1.0681 |   18312 B |      163.50 |
