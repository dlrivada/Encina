```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  27.78 μs |  1.166 μs | 0.064 μs |  1.00 |    0.00 | 0.1831 |  17.09 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  63.47 μs | 49.856 μs | 2.733 μs |  2.28 |    0.09 | 0.1221 |  19.67 KB |        1.15 |
| Invalidation_SequentialCommands | 290.56 μs | 12.901 μs | 0.707 μs | 10.46 |    0.03 | 1.9531 |  171.7 KB |       10.05 |
