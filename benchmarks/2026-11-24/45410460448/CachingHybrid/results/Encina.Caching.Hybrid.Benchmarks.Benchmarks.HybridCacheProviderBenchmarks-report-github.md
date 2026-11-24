```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.041 μs | 0.1346 μs | 0.0074 μs |  0.79 |    0.01 | 0.0057 |     616 B |        1.24 |
| ExistsAsync_True   | 1.102 μs | 0.6294 μs | 0.0345 μs |  0.84 |    0.02 | 0.0057 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.319 μs | 0.1545 μs | 0.0085 μs |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.038 μs | 0.1178 μs | 0.0065 μs |  0.79 |    0.01 | 0.0057 |     616 B |        1.24 |
