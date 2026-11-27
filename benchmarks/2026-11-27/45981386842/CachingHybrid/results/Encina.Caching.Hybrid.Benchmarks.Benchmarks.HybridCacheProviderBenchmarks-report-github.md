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
| ExistsAsync_False  | 1.312 μs | 0.0961 μs | 0.0053 μs |  0.81 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.399 μs | 0.2786 μs | 0.0153 μs |  0.86 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.624 μs | 0.1034 μs | 0.0057 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.246 μs | 0.0881 μs | 0.0048 μs |  0.77 | 0.0362 |     616 B |        1.24 |
