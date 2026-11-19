```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   918.5 ns |    27.22 ns |   1.49 ns |  1.00 |    0.00 | 0.0067 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,342.7 ns | 1,090.38 ns |  59.77 ns |  4.73 |    0.06 | 0.0191 | 0.0153 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 6,395.0 ns | 6,781.95 ns | 371.74 ns |  6.96 |    0.35 | 0.0229 | 0.0153 |    2216 B |        3.96 |
