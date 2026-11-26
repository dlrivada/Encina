```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  | 1.140 μs | 0.1920 μs | 0.0105 μs |  1.00 |    0.01 | 0.0324 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 5.837 μs | 2.5961 μs | 0.1423 μs |  5.12 |    0.12 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 8.600 μs | 3.5855 μs | 0.1965 μs |  7.54 |    0.16 | 0.1221 | 0.1068 |    2216 B |        3.96 |
