```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  53.23 ns |  0.466 ns | 0.026 ns |  0.30 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.83 ns |  7.636 ns | 0.419 ns |  0.34 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 179.38 ns | 34.083 ns | 1.868 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 130.39 ns | 16.237 ns | 0.890 ns |  0.73 |      - |         - |        0.00 |
