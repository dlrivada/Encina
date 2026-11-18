```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.61 ns | 0.668 ns | 0.037 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  52.12 ns | 9.274 ns | 0.508 ns |  0.39 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 134.61 ns | 0.622 ns | 0.034 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 100.15 ns | 1.904 ns | 0.104 ns |  0.74 |      - |         - |        0.00 |
