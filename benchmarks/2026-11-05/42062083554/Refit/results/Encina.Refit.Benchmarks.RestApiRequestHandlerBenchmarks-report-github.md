```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | Mean           | Error             | StdDev         | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------------:|------------------:|---------------:|------:|--------:|-----:|-------:|----------:|------------:|
| DirectRefitCall_Baseline    |             NA |                NA |             NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall             |             NA |                NA |             NA |     ? |       ? |    ? |     NA |        NA |           ? |
| EncinaRefitCall_Sequential5 |       7.691 μs |         0.6921 μs |      0.0379 μs |     ? |       ? |    1 | 0.5341 |   8.79 KB |           ? |
| EncinaRefitCall_Batch10     |      21.984 μs |         2.8790 μs |      0.1578 μs |     ? |       ? |    2 | 1.0986 |  18.34 KB |           ? |
| DirectRefitCall_Batch10     | 100,778.257 μs |   287,529.0719 μs | 15,760.4387 μs |     ? |       ? |    3 |      - |  94.35 KB |           ? |
| DirectRefitCall_Sequential5 | 249,224.240 μs | 1,286,645.0286 μs | 70,525.3559 μs |     ? |       ? |    4 |      - |  46.86 KB |           ? |

Benchmarks with issues:
  RestApiRequestHandlerBenchmarks.DirectRefitCall_Baseline: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RestApiRequestHandlerBenchmarks.EncinaRefitCall: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
