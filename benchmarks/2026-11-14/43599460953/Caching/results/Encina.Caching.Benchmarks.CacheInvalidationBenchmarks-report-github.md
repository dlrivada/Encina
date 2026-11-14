```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  32.78 μs |  2.379 μs | 0.130 μs |  1.00 |    0.00 | 0.1831 |  17.23 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  74.66 μs | 26.131 μs | 1.432 μs |  2.28 |    0.04 | 0.1221 |  19.31 KB |        1.12 |
| Invalidation_SequentialCommands | 350.15 μs | 15.659 μs | 0.858 μs | 10.68 |    0.04 | 1.9531 |  171.7 KB |        9.96 |
