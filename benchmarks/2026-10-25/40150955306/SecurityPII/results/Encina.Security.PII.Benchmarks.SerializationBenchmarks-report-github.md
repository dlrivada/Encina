```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |   437.8 ns |  1.99 ns |   1.32 ns |   437.3 ns |  1.00 |    0.00 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           | 1,105.3 ns |  2.79 ns |   1.66 ns | 1,105.2 ns |  2.52 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           | 2,206.0 ns |  7.74 ns |   4.60 ns | 2,204.8 ns |  5.04 |    0.02 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |   953.6 ns |  4.39 ns |   2.90 ns |   952.4 ns |  2.18 |    0.01 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           | 4,966.5 ns | 43.48 ns |  22.74 ns | 4,960.9 ns | 11.34 |    0.06 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           | 1,749.3 ns | 11.99 ns |   7.93 ns | 1,747.2 ns |  4.00 |    0.02 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           | 4,601.6 ns | 14.85 ns |   9.83 ns | 4,597.1 ns | 10.51 |    0.04 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
|                          |            |                |             |             |            |          |           |            |       |         |        |           |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |   435.4 ns |  1.59 ns |   2.37 ns |   436.5 ns |  1.00 |    0.01 | 0.0067 |     112 B |        1.00 |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          | 1,045.1 ns |  2.20 ns |   3.22 ns | 1,045.8 ns |  2.40 |    0.01 | 0.0229 |     392 B |        3.50 |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          | 2,213.3 ns |  4.03 ns |   5.65 ns | 2,211.1 ns |  5.08 |    0.03 | 0.2251 |    3784 B |       33.79 |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |   932.9 ns |  7.27 ns |  10.19 ns |   940.3 ns |  2.14 |    0.03 | 0.0134 |     232 B |        2.07 |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          | 4,951.2 ns | 70.30 ns | 103.04 ns | 4,895.2 ns | 11.37 |    0.24 | 0.4578 |    7688 B |       68.64 |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          | 1,745.3 ns |  6.81 ns |   9.55 ns | 1,749.4 ns |  4.01 |    0.03 | 0.0496 |     840 B |        7.50 |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          | 4,665.4 ns | 42.40 ns |  58.04 ns | 4,628.0 ns | 10.72 |    0.14 | 0.1144 |    2032 B |       18.14 |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          |         NA |       NA |        NA |         NA |     ? |       ? |     NA |        NA |           ? |

Benchmarks with issues:
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.MaskObject_Large: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
