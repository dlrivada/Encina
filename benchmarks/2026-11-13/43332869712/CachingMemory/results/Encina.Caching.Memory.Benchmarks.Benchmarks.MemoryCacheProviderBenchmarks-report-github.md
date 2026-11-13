```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  34.84 ns |  3.606 ns | 0.198 ns |  0.34 |      - |         - |        0.00 |
| ExistsAsync_True   |  40.77 ns | 15.911 ns | 0.872 ns |  0.39 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 103.46 ns |  5.881 ns | 0.322 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  77.24 ns | 27.006 ns | 1.480 ns |  0.75 |      - |         - |        0.00 |
