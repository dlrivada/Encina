```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    431.2 ns |   1.32 ns |  0.78 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,022.9 ns |   4.88 ns |  3.22 ns |  2.37 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,132.1 ns |  29.70 ns | 19.65 ns |  4.94 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    922.9 ns |   2.24 ns |  1.48 ns |  2.14 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  4,895.6 ns |  24.47 ns | 14.56 ns | 11.35 |    0.04 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,833.4 ns |   4.86 ns |  2.89 ns |  4.25 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,371.9 ns |  11.74 ns |  7.77 ns | 10.14 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,377.8 ns |  37.23 ns | 22.15 ns | 31.02 |    0.07 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 23,718.6 ns | 104.04 ns | 68.82 ns | 55.00 |    0.18 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    439.1 ns |  10.20 ns |  0.56 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,006.4 ns |  50.00 ns |  2.74 ns |  2.29 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,503.7 ns | 302.68 ns | 16.59 ns |  5.70 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    934.9 ns |  44.01 ns |  2.41 ns |  2.13 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  4,990.8 ns | 204.51 ns | 11.21 ns | 11.37 |    0.03 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,755.1 ns |  76.79 ns |  4.21 ns |  4.00 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,488.5 ns |  90.27 ns |  4.95 ns | 10.22 |    0.01 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,680.8 ns | 324.68 ns | 17.80 ns | 31.16 |    0.05 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 23,738.1 ns | 913.59 ns | 50.08 ns | 54.07 |    0.12 | 1.0681 |   18312 B |      163.50 |
