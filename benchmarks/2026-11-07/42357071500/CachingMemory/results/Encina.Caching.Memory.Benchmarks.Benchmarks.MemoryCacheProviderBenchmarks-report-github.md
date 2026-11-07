```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  54.09 ns | 12.088 ns | 0.663 ns |  0.28 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.13 ns |  0.889 ns | 0.049 ns |  0.32 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 190.31 ns | 13.487 ns | 0.739 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 127.03 ns | 16.575 ns | 0.909 ns |  0.67 |      - |         - |        0.00 |
