```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |  1.550 μs | 0.1170 μs | 0.0064 μs |  1.00 |    0.01 | 0.0210 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss |  7.252 μs | 1.7313 μs | 0.0949 μs |  4.68 |    0.06 | 0.0687 | 0.0610 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 11.232 μs | 1.1547 μs | 0.0633 μs |  7.25 |    0.04 | 0.0763 | 0.0610 |    2216 B |        3.96 |
