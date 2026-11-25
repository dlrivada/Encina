```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   951.2 ns |   189.1 ns |  10.37 ns |  1.00 |    0.01 | 0.0334 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,709.3 ns | 2,587.5 ns | 141.83 ns |  4.95 |    0.14 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 7,571.9 ns | 5,791.0 ns | 317.42 ns |  7.96 |    0.30 | 0.1831 | 0.1678 |    3421 B |        6.11 |
