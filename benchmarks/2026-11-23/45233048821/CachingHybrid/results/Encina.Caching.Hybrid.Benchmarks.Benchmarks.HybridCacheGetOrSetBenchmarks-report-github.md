```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.76GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |  1.523 μs | 0.1556 μs | 0.0085 μs |  1.00 |    0.01 | 0.0210 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss |  7.399 μs | 1.9022 μs | 0.1043 μs |  4.86 |    0.06 | 0.0687 | 0.0610 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 11.150 μs | 1.3546 μs | 0.0743 μs |  7.32 |    0.06 | 0.0763 | 0.0610 |    2216 B |        3.96 |
