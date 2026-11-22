```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.068 μs | 0.0149 μs | 0.0208 μs | 1.083 μs |  0.84 |    0.02 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.123 μs | 0.0048 μs | 0.0070 μs | 1.124 μs |  0.88 |    0.01 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.276 μs | 0.0054 μs | 0.0079 μs | 1.274 μs |  1.00 |    0.01 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.045 μs | 0.0073 μs | 0.0105 μs | 1.047 μs |  0.82 |    0.01 | 0.0362 |     616 B |        1.24 |
