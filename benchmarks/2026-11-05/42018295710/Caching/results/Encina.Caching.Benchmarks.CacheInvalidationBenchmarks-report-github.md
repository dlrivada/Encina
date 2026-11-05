```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.01 μs |  1.613 μs | 0.088 μs |  1.00 |    0.00 |  1.0376 |  17.38 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  57.32 μs | 13.200 μs | 0.724 μs |  1.47 |    0.02 |  1.1597 |  19.55 KB |        1.12 |
| Invalidation_SequentialCommands | 416.19 μs | 34.212 μs | 1.875 μs | 10.67 |    0.05 | 10.2539 | 171.71 KB |        9.88 |
