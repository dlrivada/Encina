```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.19 ns | 3.142 ns | 0.172 ns |  0.31 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.19 ns | 8.607 ns | 0.472 ns |  0.35 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 141.56 ns | 2.413 ns | 0.132 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.01 ns | 0.309 ns | 0.017 ns |  0.69 |      - |         - |        0.00 |
