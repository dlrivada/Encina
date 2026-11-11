```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.383 μs | 0.3525 μs | 0.0193 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.415 μs | 0.0606 μs | 0.0033 μs |  0.87 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.627 μs | 0.0280 μs | 0.0015 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.298 μs | 0.0117 μs | 0.0006 μs |  0.80 | 0.0362 |     616 B |        1.24 |
