```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   980.9 ns | 1,140.0 ns |  62.49 ns |  1.00 |    0.08 | 0.0067 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,437.8 ns | 1,683.4 ns |  92.28 ns |  4.54 |    0.26 | 0.0153 | 0.0076 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 8,325.1 ns | 7,966.9 ns | 436.70 ns |  8.51 |    0.60 | 0.0229 | 0.0153 |    2216 B |        3.96 |
