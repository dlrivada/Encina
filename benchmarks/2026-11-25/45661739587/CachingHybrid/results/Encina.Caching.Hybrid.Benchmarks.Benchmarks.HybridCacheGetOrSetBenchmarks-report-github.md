```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |---------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  | 1.244 μs |  0.1973 μs | 0.0108 μs |  1.00 |    0.01 | 0.0057 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 5.291 μs |  3.8942 μs | 0.2135 μs |  4.25 |    0.15 | 0.0153 | 0.0076 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 8.641 μs | 11.7130 μs | 0.6420 μs |  6.95 |    0.45 | 0.0229 | 0.0153 |    2216 B |        3.96 |
