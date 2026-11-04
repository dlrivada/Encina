```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.66 μs |  2.221 μs | 0.122 μs |  1.00 |    0.00 |  1.0376 |  17.32 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  65.66 μs | 59.537 μs | 3.263 μs |  1.66 |    0.07 |  1.1597 |   19.4 KB |        1.12 |
| Invalidation_SequentialCommands | 416.47 μs | 22.629 μs | 1.240 μs | 10.50 |    0.04 | 10.2539 | 171.71 KB |        9.91 |
