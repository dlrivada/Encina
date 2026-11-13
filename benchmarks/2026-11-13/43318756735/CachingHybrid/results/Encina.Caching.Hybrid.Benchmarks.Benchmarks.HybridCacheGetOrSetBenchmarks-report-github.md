```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |  1.473 μs | 0.2936 μs | 0.0161 μs |  1.00 |    0.01 | 0.0324 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss |  7.214 μs | 2.2908 μs | 0.1256 μs |  4.90 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 11.349 μs | 3.7504 μs | 0.2056 μs |  7.71 |    0.14 | 0.1221 | 0.1068 |    2216 B |        3.96 |
