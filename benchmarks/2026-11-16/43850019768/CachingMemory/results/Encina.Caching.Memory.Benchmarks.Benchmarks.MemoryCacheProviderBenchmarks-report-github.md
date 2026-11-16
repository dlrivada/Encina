```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  37.67 ns | 20.713 ns | 1.135 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  42.18 ns |  2.331 ns | 0.128 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 112.46 ns | 12.252 ns | 0.672 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  79.80 ns |  7.992 ns | 0.438 ns |  0.71 |      - |         - |        0.00 |
