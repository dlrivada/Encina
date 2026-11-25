```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.11 ns |  0.813 ns | 0.045 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  65.95 ns |  9.508 ns | 0.521 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 180.33 ns |  0.434 ns | 0.024 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 126.56 ns | 23.754 ns | 1.302 ns |  0.70 |      - |         - |        0.00 |
