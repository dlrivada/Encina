```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  48.82 ns | 30.04 ns | 1.646 ns |  0.28 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  55.35 ns | 17.79 ns | 0.975 ns |  0.31 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 177.53 ns | 40.16 ns | 2.201 ns |  1.00 |    0.02 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss | 121.30 ns | 49.04 ns | 2.688 ns |  0.68 |    0.02 |      - |         - |        0.00 |
