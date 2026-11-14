```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  34.66 ns | 10.022 ns | 0.549 ns |  0.32 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  38.86 ns |  8.721 ns | 0.478 ns |  0.36 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 107.42 ns | 90.731 ns | 4.973 ns |  1.00 |    0.06 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  79.83 ns | 36.389 ns | 1.995 ns |  0.74 |    0.03 |      - |         - |        0.00 |
