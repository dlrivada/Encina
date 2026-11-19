```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   789.7 ns |    29.75 ns |   1.63 ns |  1.00 |    0.00 | 0.0334 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,357.7 ns | 1,870.00 ns | 102.50 ns |  5.52 |    0.11 | 0.1068 | 0.1030 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 6,832.3 ns | 8,543.27 ns | 468.29 ns |  8.65 |    0.51 | 0.1297 | 0.1221 |    2216 B |        3.96 |
