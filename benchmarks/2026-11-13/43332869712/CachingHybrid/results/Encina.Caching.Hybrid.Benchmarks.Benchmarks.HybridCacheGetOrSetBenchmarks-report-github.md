```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |  1.481 μs | 0.4887 μs | 0.0268 μs |  1.00 |    0.02 | 0.0324 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss |  7.155 μs | 0.7068 μs | 0.0387 μs |  4.83 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 10.513 μs | 5.6560 μs | 0.3100 μs |  7.10 |    0.21 | 0.1221 | 0.1068 |    2216 B |        3.96 |
