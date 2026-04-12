```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |    431.1 ns |   1.72 ns |   1.14 ns |    431.0 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           |  1,004.4 ns |   9.43 ns |   5.61 ns |  1,004.7 ns |  2.33 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           |  2,146.5 ns |  43.81 ns |  28.98 ns |  2,150.8 ns |  4.98 |    0.07 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |    907.5 ns |   3.93 ns |   2.60 ns |    907.7 ns |  2.10 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           |  4,970.0 ns |  89.67 ns |  59.31 ns |  4,961.5 ns | 11.53 |    0.13 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           |  1,797.6 ns |  12.96 ns |   8.57 ns |  1,799.2 ns |  4.17 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           |  4,435.4 ns |  30.62 ns |  20.25 ns |  4,428.9 ns | 10.29 |    0.05 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           | 13,491.3 ns |  65.94 ns |  43.62 ns | 13,484.3 ns | 31.29 |    0.12 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           | 23,531.5 ns | 120.66 ns |  71.80 ns | 23,535.8 ns | 54.58 |    0.21 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |           |             |       |         |        |           |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |    437.9 ns |   2.58 ns |   3.86 ns |    437.8 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          |  1,007.2 ns |   2.67 ns |   3.99 ns |  1,006.5 ns |  2.30 |    0.02 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          |  2,191.0 ns |  45.49 ns |  63.76 ns |  2,146.1 ns |  5.00 |    0.15 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |    910.5 ns |   4.73 ns |   6.79 ns |    910.5 ns |  2.08 |    0.02 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          |  5,031.6 ns |  56.81 ns |  83.27 ns |  5,069.3 ns | 11.49 |    0.21 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          |  1,773.1 ns |  25.23 ns |  36.18 ns |  1,785.5 ns |  4.05 |    0.09 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          |  4,428.8 ns |  65.85 ns |  90.13 ns |  4,427.1 ns | 10.11 |    0.22 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          | 13,328.4 ns | 127.00 ns | 182.15 ns | 13,333.2 ns | 30.44 |    0.49 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          | 23,548.6 ns | 156.66 ns | 224.68 ns | 23,443.5 ns | 53.78 |    0.69 | 1.0681 |   18312 B |      163.50 |
