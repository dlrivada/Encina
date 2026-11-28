```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  26.17 μs |  0.435 μs | 0.024 μs |  1.00 |    0.00 | 0.1831 |   17.3 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  62.74 μs | 78.359 μs | 4.295 μs |  2.40 |    0.14 | 0.1221 |  19.23 KB |        1.11 |
| Invalidation_SequentialCommands | 279.41 μs | 19.647 μs | 1.077 μs | 10.68 |    0.04 | 1.9531 |  171.7 KB |        9.92 |
