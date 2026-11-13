```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.066 μs | 0.0473 μs | 0.0026 μs |  0.80 |    0.00 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.138 μs | 0.5733 μs | 0.0314 μs |  0.85 |    0.02 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.337 μs | 0.0607 μs | 0.0033 μs |  1.00 |    0.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.048 μs | 0.1044 μs | 0.0057 μs |  0.78 |    0.00 | 0.0362 |     616 B |        1.24 |
