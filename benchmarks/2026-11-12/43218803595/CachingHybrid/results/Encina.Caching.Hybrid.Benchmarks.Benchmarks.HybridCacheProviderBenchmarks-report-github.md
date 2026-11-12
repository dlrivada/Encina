```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.094 μs | 0.0639 μs | 0.0035 μs |  0.86 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.149 μs | 0.1655 μs | 0.0091 μs |  0.90 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.275 μs | 0.2367 μs | 0.0130 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.064 μs | 0.0134 μs | 0.0007 μs |  0.83 | 0.0362 |     616 B |        1.24 |
