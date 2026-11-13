```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.068 μs | 0.0317 μs | 0.0017 μs |  0.83 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.137 μs | 0.0502 μs | 0.0028 μs |  0.89 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.279 μs | 0.0022 μs | 0.0001 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.056 μs | 0.1340 μs | 0.0073 μs |  0.83 | 0.0362 |     616 B |        1.24 |
