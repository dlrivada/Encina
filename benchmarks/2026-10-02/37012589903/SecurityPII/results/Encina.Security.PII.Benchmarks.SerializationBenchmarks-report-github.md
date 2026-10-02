```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    406.3 ns |     0.74 ns |   0.49 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    978.4 ns |     4.02 ns |   2.39 ns |  2.41 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,193.0 ns |    56.10 ns |  37.11 ns |  5.40 |    0.09 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    903.8 ns |     2.19 ns |   1.45 ns |  2.22 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,011.1 ns |    71.52 ns |  47.31 ns | 12.33 |    0.11 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,614.7 ns |     5.57 ns |   3.31 ns |  3.97 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,950.5 ns |    30.19 ns |  19.97 ns |  9.72 |    0.05 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 12,226.1 ns |    83.62 ns |  55.31 ns | 30.09 |    0.13 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 21,860.6 ns |   249.06 ns | 164.74 ns | 53.80 |    0.39 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    414.0 ns |    12.04 ns |   0.66 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,031.6 ns |    50.23 ns |   2.75 ns |  2.49 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,171.9 ns |   653.94 ns |  35.84 ns |  5.25 |    0.08 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    900.5 ns |    18.45 ns |   1.01 ns |  2.18 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  4,940.3 ns | 1,183.80 ns |  64.89 ns | 11.93 |    0.14 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,629.2 ns |    82.79 ns |   4.54 ns |  3.94 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,039.3 ns |   591.14 ns |  32.40 ns |  9.76 |    0.07 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 12,076.7 ns |   698.16 ns |  38.27 ns | 29.17 |    0.09 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 21,835.1 ns | 1,461.34 ns |  80.10 ns | 52.75 |    0.18 | 1.0681 |   18312 B |      163.50 |
