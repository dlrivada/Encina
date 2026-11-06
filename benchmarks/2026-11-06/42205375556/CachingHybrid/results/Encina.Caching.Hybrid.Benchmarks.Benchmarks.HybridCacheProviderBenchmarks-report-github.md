```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.068 μs | 0.0462 μs | 0.0025 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.124 μs | 0.0246 μs | 0.0013 μs |  0.89 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.258 μs | 0.0430 μs | 0.0024 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.043 μs | 0.0280 μs | 0.0015 μs |  0.83 | 0.0362 |     616 B |        1.24 |
