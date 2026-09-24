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
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    435.1 ns |     0.71 ns |  0.42 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,003.5 ns |     6.07 ns |  4.02 ns |  2.31 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,248.7 ns |    28.43 ns | 18.80 ns |  5.17 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    915.1 ns |     2.44 ns |  1.45 ns |  2.10 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  4,895.1 ns |    44.81 ns | 29.64 ns | 11.25 |    0.07 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,829.5 ns |     7.10 ns |  4.22 ns |  4.20 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,457.9 ns |    12.38 ns |  8.19 ns | 10.25 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 13,276.4 ns |    64.68 ns | 42.78 ns | 30.52 |    0.10 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 24,051.5 ns |   105.50 ns | 69.78 ns | 55.28 |    0.16 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    436.4 ns |    18.11 ns |  0.99 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,020.9 ns |    28.88 ns |  1.58 ns |  2.34 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,190.3 ns |   396.34 ns | 21.72 ns |  5.02 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    919.8 ns |    57.49 ns |  3.15 ns |  2.11 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  4,941.1 ns |   825.89 ns | 45.27 ns | 11.32 |    0.09 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,851.2 ns |   389.11 ns | 21.33 ns |  4.24 |    0.04 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  4,649.9 ns |   205.45 ns | 11.26 ns | 10.65 |    0.03 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 13,512.4 ns | 1,243.56 ns | 68.16 ns | 30.96 |    0.15 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 24,416.0 ns | 1,761.11 ns | 96.53 ns | 55.95 |    0.22 | 1.0681 |   18312 B |      163.50 |
