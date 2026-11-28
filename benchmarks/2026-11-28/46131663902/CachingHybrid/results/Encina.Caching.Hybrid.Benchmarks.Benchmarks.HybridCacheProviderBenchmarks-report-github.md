```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.049 μs | 0.2343 μs | 0.0128 μs |  0.77 | 0.0057 |     616 B |        1.24 |
| ExistsAsync_True   | 1.123 μs | 0.0708 μs | 0.0039 μs |  0.82 | 0.0057 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.362 μs | 0.0787 μs | 0.0043 μs |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.064 μs | 0.0297 μs | 0.0016 μs |  0.78 | 0.0057 |     616 B |        1.24 |
