```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   356.4 ns |   4.69 ns |  2.79 ns |  1.00 |    0.01 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   824.5 ns |  16.17 ns |  9.62 ns |  2.31 |    0.03 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 1,937.4 ns |  45.66 ns | 30.20 ns |  5.44 |    0.09 | 0.0439 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   740.0 ns |   7.35 ns |  3.84 ns |  2.08 |    0.02 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 4,520.2 ns |  67.49 ns | 44.64 ns | 12.68 |    0.15 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,335.2 ns |  12.01 ns |  7.94 ns |  3.75 |    0.04 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3,692.5 ns |  59.27 ns | 35.27 ns | 10.36 |    0.12 | 0.0229 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |            |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   355.5 ns |  71.10 ns |  3.90 ns |  1.00 |    0.01 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |   868.2 ns | 188.54 ns | 10.33 ns |  2.44 |    0.03 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 1,988.3 ns | 233.68 ns | 12.81 ns |  5.59 |    0.06 | 0.0420 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   755.7 ns | 215.49 ns | 11.81 ns |  2.13 |    0.04 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 4,566.8 ns | 337.84 ns | 18.52 ns | 12.85 |    0.13 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,368.9 ns |  66.31 ns |  3.63 ns |  3.85 |    0.04 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 3,758.9 ns | 330.94 ns | 18.14 ns | 10.58 |    0.11 | 0.0229 |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
