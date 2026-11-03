```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  33.96 μs |  2.079 μs | 0.114 μs |  1.00 |    0.00 |  1.0376 |  17.87 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  49.78 μs | 49.647 μs | 2.721 μs |  1.47 |    0.07 |  1.1597 |  19.68 KB |        1.10 |
| Invalidation_SequentialCommands | 363.17 μs | 16.012 μs | 0.878 μs | 10.69 |    0.04 | 10.2539 | 171.71 KB |        9.61 |
