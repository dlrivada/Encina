```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                   | Job        | IterationCount | LaunchCount | WarmupCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------ |-----:|------:|------:|--------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     | 3           |   NA |    NA |     ? |       ? |           ? |
|                          |            |                |             |             |      |       |       |         |             |
| Serialize_Small          | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Serialize_Medium         | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| Serialize_Large          | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| SerializeRoundtrip_Small | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| SerializeRoundtrip_Large | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| ParseAndModify_Small     | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Small         | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Medium        | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Large         | MediumRun  | 15             | 2           | 10          |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  SerializationBenchmarks.Serialize_Small: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.Serialize_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.Serialize_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.SerializeRoundtrip_Small: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.SerializeRoundtrip_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.ParseAndModify_Small: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Small: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
  SerializationBenchmarks.Serialize_Small: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.Serialize_Medium: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.Serialize_Large: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.SerializeRoundtrip_Small: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.SerializeRoundtrip_Large: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.ParseAndModify_Small: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.MaskObject_Small: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.MaskObject_Medium: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  SerializationBenchmarks.MaskObject_Large: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
