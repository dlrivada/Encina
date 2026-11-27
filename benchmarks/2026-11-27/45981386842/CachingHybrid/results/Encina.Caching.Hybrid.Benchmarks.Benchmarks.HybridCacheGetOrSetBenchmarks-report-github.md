```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   899.8 ns |     72.35 ns |   3.97 ns |  1.00 |    0.01 | 0.0067 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,537.4 ns |  2,750.34 ns | 150.76 ns |  5.04 |    0.15 | 0.0153 | 0.0076 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 7,786.1 ns | 12,194.34 ns | 668.41 ns |  8.65 |    0.64 | 0.0229 | 0.0153 |    2216 B |        3.96 |
