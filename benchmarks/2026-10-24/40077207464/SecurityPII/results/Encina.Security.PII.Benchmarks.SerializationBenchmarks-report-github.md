```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   431.2 ns |   1.71 ns |  0.89 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 1,043.3 ns |   4.24 ns |  2.52 ns |  2.42 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 2,202.4 ns |  13.06 ns |  8.64 ns |  5.11 |    0.02 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   975.2 ns |   5.02 ns |  2.99 ns |  2.26 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 4,964.5 ns |  20.80 ns | 10.88 ns | 11.51 |    0.03 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,763.0 ns |   6.71 ns |  4.44 ns |  4.09 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 4,672.1 ns |  10.45 ns |  6.22 ns | 10.83 |    0.03 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |            |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   478.5 ns |  29.06 ns |  1.59 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           | 1,032.7 ns |  41.38 ns |  2.27 ns |  2.16 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 2,195.2 ns | 275.07 ns | 15.08 ns |  4.59 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   933.1 ns |  37.16 ns |  2.04 ns |  1.95 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 5,022.1 ns | 458.18 ns | 25.11 ns | 10.49 |    0.05 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,798.0 ns |  47.48 ns |  2.60 ns |  3.76 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 4,879.7 ns | 147.64 ns |  8.09 ns | 10.20 |    0.03 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
