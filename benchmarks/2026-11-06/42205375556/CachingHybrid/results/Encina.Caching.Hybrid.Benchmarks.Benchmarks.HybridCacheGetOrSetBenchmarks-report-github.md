```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |  1.496 μs | 0.0735 μs | 0.0040 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss |  7.049 μs | 3.9119 μs | 0.2144 μs |  4.71 |    0.12 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 10.721 μs | 6.3082 μs | 0.3458 μs |  7.17 |    0.20 | 0.1221 | 0.1068 |    2216 B |        3.96 |
