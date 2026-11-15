```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  34.31 ns |  9.745 ns | 0.534 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  38.49 ns |  5.296 ns | 0.290 ns |  0.35 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 108.69 ns | 21.220 ns | 1.163 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  73.61 ns |  4.114 ns | 0.226 ns |  0.68 |      - |         - |        0.00 |
