```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |    439.0 ns |   1.14 ns |   0.60 ns |    439.2 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           |  1,049.8 ns |   2.68 ns |   1.60 ns |  1,049.9 ns |  2.39 |    0.00 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           |  2,171.4 ns |  30.58 ns |  18.20 ns |  2,163.7 ns |  4.95 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |    940.9 ns |   1.58 ns |   0.94 ns |    940.8 ns |  2.14 |    0.00 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           |  4,958.5 ns |  50.18 ns |  33.19 ns |  4,954.4 ns | 11.29 |    0.07 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           |  1,752.0 ns |   4.55 ns |   2.71 ns |  1,752.6 ns |  3.99 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           |  4,424.5 ns |  12.17 ns |   8.05 ns |  4,423.3 ns | 10.08 |    0.02 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           | 13,552.3 ns |  40.94 ns |  27.08 ns | 13,546.6 ns | 30.87 |    0.07 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           | 23,829.2 ns |  72.22 ns |  47.77 ns | 23,825.9 ns | 54.28 |    0.13 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |           |             |       |         |        |           |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |    436.0 ns |   4.98 ns |   7.14 ns |    435.8 ns |  1.00 |    0.02 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          |  1,005.9 ns |   5.47 ns |   7.49 ns |  1,001.6 ns |  2.31 |    0.04 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          |  2,155.1 ns |  15.63 ns |  22.90 ns |  2,155.6 ns |  4.94 |    0.09 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |    952.3 ns |  11.41 ns |  15.99 ns |    965.3 ns |  2.18 |    0.05 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          |  4,856.9 ns |  15.86 ns |  22.23 ns |  4,858.7 ns | 11.14 |    0.19 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          |  1,785.3 ns |   8.93 ns |  12.81 ns |  1,786.2 ns |  4.10 |    0.07 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          |  4,423.3 ns |  18.02 ns |  25.84 ns |  4,431.3 ns | 10.15 |    0.17 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          | 13,703.0 ns |  26.13 ns |  38.31 ns | 13,718.3 ns | 31.44 |    0.51 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          | 24,130.0 ns | 233.04 ns | 341.58 ns | 24,388.2 ns | 55.36 |    1.18 | 1.0681 |   18312 B |      163.50 |
