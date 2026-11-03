```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  34.21 ns | 7.476 ns | 0.410 ns |  0.34 |      - |         - |        0.00 |
| ExistsAsync_True   |  38.67 ns | 3.646 ns | 0.200 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 100.78 ns | 7.069 ns | 0.387 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  71.65 ns | 5.480 ns | 0.300 ns |  0.71 |      - |         - |        0.00 |
