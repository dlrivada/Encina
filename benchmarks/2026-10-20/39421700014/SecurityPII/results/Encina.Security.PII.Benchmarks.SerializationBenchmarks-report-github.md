```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   262.8 ns |     2.31 ns |   1.21 ns |  1.00 |    0.01 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   640.1 ns |    37.93 ns |  25.09 ns |  2.44 |    0.09 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 1,645.3 ns |    67.30 ns |  44.52 ns |  6.26 |    0.16 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   558.4 ns |    14.41 ns |   8.58 ns |  2.13 |    0.03 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3,818.5 ns |   325.34 ns | 215.19 ns | 14.53 |    0.78 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,038.3 ns |    79.17 ns |  52.36 ns |  3.95 |    0.19 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 2,872.4 ns |   260.23 ns | 172.12 ns | 10.93 |    0.63 | 0.0229 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |          NA |        NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |          NA |        NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |            |             |           |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   259.4 ns |     1.97 ns |   0.11 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |   620.3 ns |    18.55 ns |   1.02 ns |  2.39 |    0.00 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 1,647.7 ns | 1,402.40 ns |  76.87 ns |  6.35 |    0.26 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   569.3 ns |   121.93 ns |   6.68 ns |  2.19 |    0.02 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 3,679.0 ns |   217.56 ns |  11.93 ns | 14.18 |    0.04 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,105.8 ns | 1,234.09 ns |  67.64 ns |  4.26 |    0.23 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 3,032.4 ns | 4,419.65 ns | 242.26 ns | 11.69 |    0.81 | 0.0229 |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |          NA |        NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |          NA |        NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
