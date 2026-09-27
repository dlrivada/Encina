```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    309.7 ns |     0.61 ns |   0.36 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    755.0 ns |     3.36 ns |   2.00 ns |  2.44 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,723.1 ns |    20.27 ns |  12.06 ns |  5.56 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    682.6 ns |     2.93 ns |   1.74 ns |  2.20 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,996.2 ns |    64.11 ns |  33.53 ns | 12.91 |    0.10 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,247.1 ns |    12.52 ns |   6.55 ns |  4.03 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,126.4 ns |    13.72 ns |   9.08 ns | 10.10 |    0.03 | 0.1030 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |  9,557.7 ns |    29.76 ns |  17.71 ns | 30.87 |    0.06 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 17,104.2 ns |   171.37 ns | 101.98 ns | 55.24 |    0.32 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    339.9 ns |     9.04 ns |   0.50 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    765.0 ns |     1.48 ns |   0.08 ns |  2.25 |    0.00 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,758.4 ns |   472.98 ns |  25.93 ns |  5.17 |    0.07 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    734.1 ns |    20.93 ns |   1.15 ns |  2.16 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,847.6 ns |   419.50 ns |  22.99 ns | 11.32 |    0.06 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,247.7 ns |    41.34 ns |   2.27 ns |  3.67 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,172.3 ns | 1,243.64 ns |  68.17 ns |  9.33 |    0.17 | 0.1030 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |  9,630.4 ns |   713.11 ns |  39.09 ns | 28.33 |    0.11 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 17,038.9 ns |   992.63 ns |  54.41 ns | 50.13 |    0.15 | 1.0681 |   18312 B |      163.50 |
