```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  | 1.252 μs | 0.7471 μs | 0.0410 μs |  1.00 |    0.04 | 0.0057 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 5.577 μs | 2.7217 μs | 0.1492 μs |  4.46 |    0.16 | 0.0153 | 0.0076 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 7.380 μs | 5.2232 μs | 0.2863 μs |  5.90 |    0.26 | 0.0305 | 0.0153 |    3418 B |        6.10 |
