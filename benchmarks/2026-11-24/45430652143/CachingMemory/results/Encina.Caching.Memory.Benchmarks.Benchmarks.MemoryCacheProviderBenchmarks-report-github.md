```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.70 ns |   0.923 ns | 0.051 ns |  0.32 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.09 ns |   2.817 ns | 0.154 ns |  0.36 |    0.02 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 138.96 ns | 132.772 ns | 7.278 ns |  1.00 |    0.06 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  98.17 ns |   9.796 ns | 0.537 ns |  0.71 |    0.03 |      - |         - |        0.00 |
