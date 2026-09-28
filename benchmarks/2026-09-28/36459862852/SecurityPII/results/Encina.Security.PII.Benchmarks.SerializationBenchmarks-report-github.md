```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    461.5 ns |     3.12 ns |   2.07 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |  1,070.2 ns |    24.17 ns |  15.99 ns |  2.32 |    0.03 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  2,819.5 ns |   185.67 ns | 122.81 ns |  6.11 |    0.26 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    960.4 ns |    10.30 ns |   6.81 ns |  2.08 |    0.02 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  6,107.1 ns |   257.22 ns | 170.14 ns | 13.23 |    0.36 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |  1,928.2 ns |   100.75 ns |  66.64 ns |  4.18 |    0.14 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  4,879.1 ns |    72.07 ns |  37.69 ns | 10.57 |    0.09 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 14,872.3 ns |   615.17 ns | 406.90 ns | 32.23 |    0.85 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 26,852.0 ns |   673.53 ns | 445.50 ns | 58.18 |    0.95 | 1.0681 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    460.2 ns |    51.34 ns |   2.81 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |  1,119.4 ns |   639.86 ns |  35.07 ns |  2.43 |    0.07 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  2,689.0 ns | 3,436.36 ns | 188.36 ns |  5.84 |    0.36 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    966.6 ns |   144.18 ns |   7.90 ns |  2.10 |    0.02 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  5,803.9 ns | 2,432.04 ns | 133.31 ns | 12.61 |    0.26 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |  1,897.7 ns |   479.99 ns |  26.31 ns |  4.12 |    0.05 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  5,069.6 ns | 1,179.71 ns |  64.66 ns | 11.02 |    0.14 | 0.0992 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           | 14,961.5 ns | 3,512.76 ns | 192.55 ns | 32.51 |    0.40 | 0.3815 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 27,072.3 ns | 4,415.26 ns | 242.02 ns | 58.83 |    0.55 | 1.0681 |   18312 B |      163.50 |
