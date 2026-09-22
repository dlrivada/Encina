```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |    260.1 ns |     1.06 ns |   0.56 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |    625.9 ns |    27.31 ns |  16.25 ns |  2.41 |    0.06 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |  1,515.7 ns |    67.30 ns |  44.52 ns |  5.83 |    0.16 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |    592.0 ns |    53.89 ns |  35.64 ns |  2.28 |    0.13 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |  3,434.0 ns |   159.81 ns |  95.10 ns | 13.20 |    0.35 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |    988.0 ns |    11.13 ns |   6.62 ns |  3.80 |    0.03 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |  2,565.3 ns |    34.00 ns |  17.78 ns |  9.86 |    0.07 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |  8,877.1 ns |   168.23 ns |  87.99 ns | 34.13 |    0.33 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 16,403.7 ns |   289.75 ns | 151.54 ns | 63.07 |    0.56 | 0.2136 |   18312 B |      163.50 |
|                          |            |                |             |             |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |    258.5 ns |     6.58 ns |   0.36 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |    640.9 ns |   236.89 ns |  12.98 ns |  2.48 |    0.04 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           |  1,445.4 ns | 1,182.56 ns |  64.82 ns |  5.59 |    0.22 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |    551.0 ns |    49.85 ns |   2.73 ns |  2.13 |    0.01 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |  3,420.1 ns | 1,192.57 ns |  65.37 ns | 13.23 |    0.22 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |    975.2 ns |    50.35 ns |   2.76 ns |  3.77 |    0.01 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           |  2,605.7 ns |   274.61 ns |  15.05 ns | 10.08 |    0.05 | 0.0191 |    1752 B |       15.64 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |  8,959.2 ns | 3,861.38 ns | 211.66 ns | 34.65 |    0.71 | 0.0763 |    6504 B |       58.07 |
| MaskObject_Large         | ShortRun   | 3              | 1           | 16,304.3 ns |   903.28 ns |  49.51 ns | 63.07 |    0.18 | 0.2136 |   18312 B |      163.50 |
