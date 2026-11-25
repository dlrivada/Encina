```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   836.8 ns |   185.7 ns |  10.18 ns |  1.00 |    0.01 | 0.0334 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,497.1 ns |   564.9 ns |  30.96 ns |  5.37 |    0.06 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 7,526.4 ns | 9,315.2 ns | 510.60 ns |  9.00 |    0.54 | 0.1297 | 0.1221 |    2216 B |        3.96 |
