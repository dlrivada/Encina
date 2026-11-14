```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  37.46 μs |  5.810 μs | 0.318 μs |  1.00 |    0.01 |  1.0376 |  17.53 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  53.86 μs | 64.405 μs | 3.530 μs |  1.44 |    0.08 |  1.1597 |  19.31 KB |        1.10 |
| Invalidation_SequentialCommands | 413.74 μs |  6.400 μs | 0.351 μs | 11.05 |    0.08 | 10.2539 |  171.7 KB |        9.79 |
