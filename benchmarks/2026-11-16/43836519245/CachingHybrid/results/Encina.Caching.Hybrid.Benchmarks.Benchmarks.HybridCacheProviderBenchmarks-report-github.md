```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.077 μs | 0.0223 μs | 0.0012 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.155 μs | 0.1346 μs | 0.0074 μs |  0.91 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.271 μs | 0.0221 μs | 0.0012 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.044 μs | 0.0428 μs | 0.0023 μs |  0.82 | 0.0362 |     616 B |        1.24 |
