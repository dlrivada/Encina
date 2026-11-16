```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.85 μs |  6.130 μs | 0.336 μs |  1.00 |    0.01 |  1.0376 |  17.19 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  53.51 μs | 32.767 μs | 1.796 μs |  1.34 |    0.04 |  1.1597 |  19.75 KB |        1.15 |
| Invalidation_SequentialCommands | 410.82 μs |  8.778 μs | 0.481 μs | 10.31 |    0.08 | 10.2539 | 171.71 KB |        9.99 |
