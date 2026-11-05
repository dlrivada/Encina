```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  54.23 ns |  0.378 ns | 0.021 ns |  0.30 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.33 ns |  1.273 ns | 0.070 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 182.87 ns |  5.508 ns | 0.302 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 128.82 ns | 53.325 ns | 2.923 ns |  0.70 |      - |         - |        0.00 |
