```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |-----:|------:|------:|--------:|------------:|
| Serialize_Small          | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Serialize_Medium         | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| Serialize_Large          | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| SerializeRoundtrip_Small | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| SerializeRoundtrip_Large | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| ParseAndModify_Small     | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Small         | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Medium        | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Large         | Job-YFEFPZ | 10             | Default     |   NA |    NA |     ? |       ? |           ? |
|                          |            |                |             |      |       |       |         |             |
| Serialize_Small          | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Serialize_Medium         | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| Serialize_Large          | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| SerializeRoundtrip_Small | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| SerializeRoundtrip_Large | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| ParseAndModify_Small     | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Small         | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Medium        | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |
| MaskObject_Large         | ShortRun   | 3              | 1           |   NA |    NA |     ? |       ? |           ? |

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
  SerializationBenchmarks.Serialize_Small: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.Serialize_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.Serialize_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.SerializeRoundtrip_Small: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.SerializeRoundtrip_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.ParseAndModify_Small: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Small: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Medium: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  SerializationBenchmarks.MaskObject_Large: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
