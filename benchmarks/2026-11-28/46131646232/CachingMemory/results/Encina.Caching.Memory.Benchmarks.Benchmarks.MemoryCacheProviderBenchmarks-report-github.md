```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  50.14 ns |  1.368 ns | 0.075 ns |  0.27 |      - |         - |        0.00 |
| ExistsAsync_True   |  56.45 ns |  2.488 ns | 0.136 ns |  0.30 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 186.93 ns | 26.826 ns | 1.470 ns |  1.00 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss | 125.34 ns | 16.253 ns | 0.891 ns |  0.67 |      - |         - |        0.00 |
