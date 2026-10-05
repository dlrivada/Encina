```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    365.9 ns |     8.63 ns |   5.71 ns |  1.00 |    0.02 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    862.6 ns |    14.12 ns |   9.34 ns |  2.36 |    0.04 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,006.2 ns |    45.31 ns |  29.97 ns |  5.48 |    0.11 | 0.0420 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    769.2 ns |     9.52 ns |   6.30 ns |  2.10 |    0.04 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  4,682.3 ns |    66.34 ns |  39.48 ns | 12.80 |    0.22 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,409.0 ns |    23.25 ns |  15.38 ns |  3.85 |    0.07 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  3,534.2 ns |    67.78 ns |  44.83 ns |  9.66 |    0.19 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 11,658.2 ns |   150.58 ns |  78.76 ns | 31.87 |    0.52 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 22,068.5 ns |   241.11 ns | 159.48 ns | 60.33 |    0.99 | 0.2136 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    387.4 ns |    86.84 ns |   4.76 ns |  1.00 |    0.02 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    873.2 ns |   190.48 ns |  10.44 ns |  2.25 |    0.03 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,980.2 ns |   215.26 ns |  11.80 ns |  5.11 |    0.06 | 0.0420 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    761.2 ns |   178.30 ns |   9.77 ns |  1.96 |    0.03 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  4,749.8 ns | 1,684.85 ns |  92.35 ns | 12.26 |    0.24 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,399.8 ns |   387.06 ns |  21.22 ns |  3.61 |    0.06 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,485.2 ns |   135.49 ns |   7.43 ns |  9.00 |    0.10 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 11,688.6 ns | 3,312.58 ns | 181.57 ns | 30.17 |    0.52 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 21,110.3 ns | 4,200.04 ns | 230.22 ns | 54.50 |    0.78 | 0.2136 |   18312 B |      163.50 |
