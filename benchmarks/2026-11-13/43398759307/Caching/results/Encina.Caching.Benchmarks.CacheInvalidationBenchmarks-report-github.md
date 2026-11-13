```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  33.31 μs |   6.408 μs | 0.351 μs |  1.00 |    0.01 | 0.1831 |  17.18 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  74.32 μs |  48.276 μs | 2.646 μs |  2.23 |    0.07 | 0.1221 |  19.53 KB |        1.14 |
| Invalidation_SequentialCommands | 353.66 μs | 100.226 μs | 5.494 μs | 10.62 |    0.17 | 1.9531 | 171.71 KB |        9.99 |
