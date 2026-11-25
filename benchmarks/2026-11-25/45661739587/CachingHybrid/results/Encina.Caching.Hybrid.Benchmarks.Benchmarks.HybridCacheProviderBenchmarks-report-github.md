```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.339 μs | 0.1595 μs | 0.0087 μs |  0.81 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.400 μs | 0.3569 μs | 0.0196 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.656 μs | 0.1670 μs | 0.0092 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.325 μs | 0.2055 μs | 0.0113 μs |  0.80 | 0.0362 |     616 B |        1.24 |
