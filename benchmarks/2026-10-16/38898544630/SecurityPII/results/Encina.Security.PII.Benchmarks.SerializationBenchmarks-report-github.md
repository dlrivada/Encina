```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   220.2 ns |     3.99 ns |   2.09 ns |  1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   534.7 ns |     5.22 ns |   2.73 ns |  2.43 |    0.02 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 1,283.5 ns |     9.50 ns |   5.65 ns |  5.83 |    0.06 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   555.5 ns |     3.94 ns |   2.61 ns |  2.52 |    0.03 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 2,790.9 ns |    51.69 ns |  27.04 ns | 12.68 |    0.16 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,003.5 ns |    19.36 ns |  10.13 ns |  4.56 |    0.06 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 2,585.4 ns |    37.80 ns |  25.00 ns | 11.74 |    0.15 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |          NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |          NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                          |            |                |             |            |             |           |       |         |        |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   237.0 ns |    84.97 ns |   4.66 ns |  1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |   590.2 ns |    58.18 ns |   3.19 ns |  2.49 |    0.04 | 0.0229 |      - |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 1,139.4 ns |   147.55 ns |   8.09 ns |  4.81 |    0.09 | 0.2251 |      - |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   545.2 ns |    49.05 ns |   2.69 ns |  2.30 |    0.04 | 0.0134 |      - |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 2,704.9 ns | 1,231.86 ns |  67.52 ns | 11.42 |    0.32 | 0.4578 | 0.0038 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,083.1 ns | 2,893.57 ns | 158.61 ns |  4.57 |    0.59 | 0.0496 |      - |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 2,519.7 ns | 3,767.97 ns | 206.54 ns | 10.63 |    0.78 | 0.1183 |      - |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |          NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |          NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
