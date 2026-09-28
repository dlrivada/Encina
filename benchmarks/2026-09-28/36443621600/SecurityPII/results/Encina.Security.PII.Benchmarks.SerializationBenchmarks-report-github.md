```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    392.1 ns |      3.84 ns |   2.28 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    965.6 ns |      4.84 ns |   3.20 ns |  2.46 |    0.02 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,266.5 ns |     14.49 ns |   9.58 ns |  5.78 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    852.8 ns |      2.63 ns |   1.56 ns |  2.17 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  5,167.4 ns |     48.80 ns |  25.52 ns | 13.18 |    0.09 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,625.5 ns |      6.58 ns |   3.44 ns |  4.15 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,080.7 ns |     12.13 ns |   8.02 ns | 10.41 |    0.06 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 12,105.4 ns |     55.47 ns |  36.69 ns | 30.87 |    0.19 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 22,049.0 ns |    129.52 ns |  77.08 ns | 56.23 |    0.36 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |              |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    435.2 ns |     38.13 ns |   2.09 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    977.9 ns |     55.91 ns |   3.06 ns |  2.25 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,284.0 ns |     73.24 ns |   4.01 ns |  5.25 |    0.02 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    892.8 ns |     10.60 ns |   0.58 ns |  2.05 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,015.8 ns |  1,195.53 ns |  65.53 ns | 11.53 |    0.14 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,612.5 ns |     87.59 ns |   4.80 ns |  3.71 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  3,992.9 ns |    435.86 ns |  23.89 ns |  9.18 |    0.06 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 12,139.4 ns |  1,493.04 ns |  81.84 ns | 27.90 |    0.20 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 22,554.6 ns | 13,416.10 ns | 735.38 ns | 51.83 |    1.48 | 1.0681 |   18312 B |      163.50 |
