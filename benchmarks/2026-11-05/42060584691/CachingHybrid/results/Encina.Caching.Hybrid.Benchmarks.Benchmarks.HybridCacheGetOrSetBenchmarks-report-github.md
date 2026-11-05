```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |  1.445 μs | 0.0514 μs | 0.0028 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss |  7.135 μs | 2.4422 μs | 0.1339 μs |  4.94 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 10.651 μs | 3.6764 μs | 0.2015 μs |  7.37 |    0.12 | 0.1221 | 0.1068 |    2216 B |        3.96 |
