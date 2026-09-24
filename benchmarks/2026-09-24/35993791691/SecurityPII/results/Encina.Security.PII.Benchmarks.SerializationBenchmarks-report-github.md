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
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    449.0 ns |     1.14 ns |   0.75 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,052.2 ns |     3.32 ns |   1.73 ns |  2.34 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,388.9 ns |    12.89 ns |   8.53 ns |  5.32 |    0.02 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    931.6 ns |     1.67 ns |   0.87 ns |  2.07 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,076.3 ns |    17.50 ns |  10.42 ns | 11.31 |    0.03 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,829.7 ns |     6.25 ns |   3.72 ns |  4.07 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,723.0 ns |    13.45 ns |   8.00 ns | 10.52 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,786.4 ns |    38.59 ns |  25.53 ns | 30.70 |    0.07 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,942.6 ns |   370.19 ns | 244.86 ns | 55.55 |    0.53 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    453.0 ns |    37.87 ns |   2.08 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,046.7 ns |    47.41 ns |   2.60 ns |  2.31 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,361.4 ns |   186.22 ns |  10.21 ns |  5.21 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    937.7 ns |    42.46 ns |   2.33 ns |  2.07 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,166.2 ns | 1,123.99 ns |  61.61 ns | 11.40 |    0.13 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,813.3 ns |    89.17 ns |   4.89 ns |  4.00 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,867.6 ns |   124.42 ns |   6.82 ns | 10.75 |    0.04 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,854.9 ns | 1,417.84 ns |  77.72 ns | 30.59 |    0.19 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 24,628.3 ns | 1,895.30 ns | 103.89 ns | 54.37 |    0.29 | 1.0681 |   18312 B |      163.50 |
