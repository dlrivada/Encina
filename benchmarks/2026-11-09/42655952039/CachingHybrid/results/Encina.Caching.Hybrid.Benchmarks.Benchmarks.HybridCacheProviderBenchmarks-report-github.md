```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.086 μs | 0.0375 μs | 0.0021 μs |  0.85 |    0.01 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.155 μs | 0.0896 μs | 0.0049 μs |  0.90 |    0.01 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.278 μs | 0.3768 μs | 0.0207 μs |  1.00 |    0.02 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.052 μs | 0.0252 μs | 0.0014 μs |  0.82 |    0.01 | 0.0362 |     616 B |        1.24 |
