```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |    403.8 ns |   2.17 ns |   1.13 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           |    973.5 ns |   3.85 ns |   2.29 ns |  2.41 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           |  2,187.4 ns |  35.13 ns |  23.23 ns |  5.42 |    0.06 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |    887.8 ns |   2.12 ns |   1.40 ns |  2.20 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           |  5,153.6 ns |  53.48 ns |  35.37 ns | 12.76 |    0.09 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           |  1,620.2 ns |   5.75 ns |   3.80 ns |  4.01 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           |  4,016.6 ns |  27.30 ns |  16.24 ns |  9.95 |    0.05 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           | 12,193.5 ns |  61.11 ns |  36.36 ns | 30.20 |    0.12 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           | 22,337.9 ns | 353.83 ns | 234.04 ns | 55.32 |    0.57 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |           |       |         |        |           |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |    415.0 ns |   4.07 ns |   5.83 ns |  1.00 |    0.02 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          |    968.0 ns |   1.86 ns |   2.48 ns |  2.33 |    0.03 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          |  2,197.1 ns |  17.35 ns |  25.43 ns |  5.30 |    0.09 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |    879.5 ns |   2.04 ns |   2.93 ns |  2.12 |    0.03 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          |  5,049.5 ns |  28.48 ns |  42.63 ns | 12.17 |    0.20 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          |  1,635.6 ns |  16.07 ns |  23.56 ns |  3.94 |    0.08 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          |  4,006.2 ns |   9.00 ns |  12.62 ns |  9.66 |    0.14 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          | 12,075.0 ns |  23.78 ns |  34.11 ns | 29.10 |    0.41 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          | 21,888.4 ns |  75.83 ns | 111.15 ns | 52.75 |    0.77 | 1.0681 |   18312 B |      163.50 |
