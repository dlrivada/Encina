```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.04 ns |  2.745 ns | 0.150 ns |  0.32 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.03 ns |  1.850 ns | 0.101 ns |  0.36 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 137.28 ns | 59.238 ns | 3.247 ns |  1.00 |    0.03 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  96.87 ns |  2.672 ns | 0.146 ns |  0.71 |    0.01 |      - |         - |        0.00 |
