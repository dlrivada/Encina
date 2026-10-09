```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   372.2 ns |   1.70 ns |  1.01 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   867.9 ns |   2.01 ns |  1.33 ns |  2.33 |    0.01 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 1,953.8 ns |  27.90 ns | 16.60 ns |  5.25 |    0.04 | 0.0420 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   773.4 ns |   1.40 ns |  0.83 ns |  2.08 |    0.01 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 4,660.0 ns |  18.43 ns | 10.97 ns | 12.52 |    0.04 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 1,410.8 ns |   2.82 ns |  1.68 ns |  3.79 |    0.01 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3,808.1 ns |   7.00 ns |  4.63 ns | 10.23 |    0.03 | 0.0229 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |            |           |          |       |         |        |           |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   382.1 ns |   3.23 ns |  0.18 ns |  1.00 |    0.00 | 0.0010 |     112 B |        1.00 |
| Serialize_Medium         | ShortRun   | 3              | 1           |   857.6 ns |  54.48 ns |  2.99 ns |  2.24 |    0.01 | 0.0038 |     392 B |        3.50 |
| Serialize_Large          | ShortRun   | 3              | 1           | 1,974.9 ns | 264.70 ns | 14.51 ns |  5.17 |    0.03 | 0.0420 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   785.2 ns |  27.93 ns |  1.53 ns |  2.05 |    0.00 | 0.0019 |     232 B |        2.07 |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           | 4,614.4 ns | 324.60 ns | 17.79 ns | 12.08 |    0.04 | 0.0916 |    7688 B |       68.64 |
| ParseAndModify_Small     | ShortRun   | 3              | 1           | 1,418.7 ns |  58.04 ns |  3.18 ns |  3.71 |    0.01 | 0.0095 |     840 B |        7.50 |
| MaskObject_Small         | ShortRun   | 3              | 1           | 3,846.5 ns | 131.85 ns |  7.23 ns | 10.07 |    0.02 | 0.0229 |    2032 B |       18.14 |
| MaskObject_Medium        | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |         NA |        NA |       NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
