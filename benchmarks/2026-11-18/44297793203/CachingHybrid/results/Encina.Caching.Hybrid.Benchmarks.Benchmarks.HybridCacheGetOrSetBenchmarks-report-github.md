```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  | 1.099 μs | 0.0503 μs | 0.0028 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 5.491 μs | 2.1715 μs | 0.1190 μs |  4.99 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 8.123 μs | 2.4769 μs | 0.1358 μs |  7.39 |    0.11 | 0.1831 | 0.1678 |    3417 B |        6.10 |
