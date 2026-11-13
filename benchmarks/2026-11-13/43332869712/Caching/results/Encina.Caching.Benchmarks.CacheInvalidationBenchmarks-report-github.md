```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.39GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  17.85 μs |  3.727 μs | 0.204 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |  17.41 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  25.51 μs | 18.568 μs | 1.018 μs |  1.43 |    0.05 |  1.1597 | 0.0305 |  19.42 KB |        1.12 |
| Invalidation_SequentialCommands | 190.50 μs | 14.868 μs | 0.815 μs | 10.67 |    0.11 | 10.4980 | 0.2441 |  171.7 KB |        9.86 |
