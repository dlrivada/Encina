```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   915.6 ns |     5.39 ns |   0.30 ns |  1.00 |    0.00 | 0.0067 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,276.1 ns | 2,544.21 ns | 139.46 ns |  4.67 |    0.13 | 0.0153 | 0.0076 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 7,611.3 ns | 8,220.13 ns | 450.57 ns |  8.31 |    0.43 | 0.0229 | 0.0153 |    2216 B |        3.96 |
