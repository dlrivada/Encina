```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.37 ns |  3.851 ns | 0.211 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.03 ns |  1.175 ns | 0.064 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 179.20 ns |  6.325 ns | 0.347 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 126.60 ns | 24.460 ns | 1.341 ns |  0.71 |      - |         - |        0.00 |
