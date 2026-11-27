```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  51.43 ns | 6.388 ns | 0.350 ns |  0.27 |      - |         - |        0.00 |
| ExistsAsync_True   |  57.77 ns | 1.901 ns | 0.104 ns |  0.30 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 191.99 ns | 6.602 ns | 0.362 ns |  1.00 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss | 129.88 ns | 7.622 ns | 0.418 ns |  0.68 |      - |         - |        0.00 |
