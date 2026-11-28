```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.38 ns |  3.942 ns | 0.216 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.13 ns |  1.874 ns | 0.103 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 182.70 ns | 19.834 ns | 1.087 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 124.91 ns |  7.418 ns | 0.407 ns |  0.68 |      - |         - |        0.00 |
