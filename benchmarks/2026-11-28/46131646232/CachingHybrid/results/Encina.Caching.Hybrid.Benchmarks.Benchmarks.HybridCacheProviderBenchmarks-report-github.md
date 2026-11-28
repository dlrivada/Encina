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
| ExistsAsync_False  | 1.099 μs | 0.0423 μs | 0.0023 μs |  0.87 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.150 μs | 0.2107 μs | 0.0115 μs |  0.91 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.265 μs | 0.0476 μs | 0.0026 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.035 μs | 0.0505 μs | 0.0028 μs |  0.82 | 0.0362 |     616 B |        1.24 |
