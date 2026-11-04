```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   853.4 ns |    171.9 ns |   9.42 ns |  1.00 |    0.01 | 0.0334 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,789.0 ns |  7,398.8 ns | 405.55 ns |  5.61 |    0.42 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 7,502.7 ns | 13,309.1 ns | 729.52 ns |  8.79 |    0.75 | 0.1678 | 0.1526 |    3387 B |        6.05 |
