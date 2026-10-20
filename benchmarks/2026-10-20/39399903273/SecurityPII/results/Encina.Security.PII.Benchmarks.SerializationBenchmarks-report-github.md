```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   442.8 ns |     0.96 ns |  0.50 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 1,060.1 ns |     3.68 ns |  2.19 ns |  2.39 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 2,424.2 ns |    39.67 ns | 26.24 ns |  5.47 |    0.06 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   972.4 ns |     3.79 ns |  2.50 ns |  2.20 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 5,350.7 ns |    45.33 ns | 29.98 ns | 12.08 |    0.07 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,776.5 ns |     9.37 ns |  6.20 ns |  4.01 |    0.01 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 4,900.5 ns |     9.84 ns |  6.51 ns | 11.07 |    0.02 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |          NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |          NA |       NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |            |             |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   440.7 ns |    62.96 ns |  3.45 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           | 1,078.4 ns |   216.05 ns | 11.84 ns |  2.45 |    0.03 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 2,416.0 ns |   146.03 ns |  8.00 ns |  5.48 |    0.04 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   970.6 ns |    87.60 ns |  4.80 ns |  2.20 |    0.02 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 5,167.5 ns | 1,015.47 ns | 55.66 ns | 11.73 |    0.14 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,830.6 ns |    66.22 ns |  3.63 ns |  4.15 |    0.03 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 4,838.2 ns |   217.98 ns | 11.95 ns | 10.98 |    0.08 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |          NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |          NA |       NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
