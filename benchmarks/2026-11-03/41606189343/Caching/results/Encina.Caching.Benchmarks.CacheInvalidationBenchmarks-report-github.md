```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.34 μs |  4.454 μs | 0.244 μs |  1.00 |    0.01 |  1.0376 |  17.35 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  66.34 μs | 98.540 μs | 5.401 μs |  1.69 |    0.12 |  1.0986 |  19.42 KB |        1.12 |
| Invalidation_SequentialCommands | 420.07 μs | 26.827 μs | 1.470 μs | 10.68 |    0.07 | 10.2539 |  171.7 KB |        9.89 |
