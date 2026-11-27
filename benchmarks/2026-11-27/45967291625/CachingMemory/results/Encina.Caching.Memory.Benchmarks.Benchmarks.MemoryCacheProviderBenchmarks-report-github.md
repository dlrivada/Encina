```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  45.06 ns | 17.346 ns | 0.951 ns |  0.34 |      - |         - |        0.00 |
| ExistsAsync_True   |  56.60 ns |  6.033 ns | 0.331 ns |  0.42 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 133.46 ns |  1.267 ns | 0.069 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.98 ns |  0.130 ns | 0.007 ns |  0.73 |      - |         - |        0.00 |
