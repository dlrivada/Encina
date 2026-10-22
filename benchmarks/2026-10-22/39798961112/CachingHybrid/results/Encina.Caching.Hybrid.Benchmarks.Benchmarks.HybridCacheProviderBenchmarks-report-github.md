```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean     | Error     | StdDev    | Median   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.075 μs | 0.0125 μs | 0.0172 μs | 1.074 μs |  0.83 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.148 μs | 0.0081 μs | 0.0119 μs | 1.141 μs |  0.89 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.292 μs | 0.0066 μs | 0.0093 μs | 1.294 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.064 μs | 0.0075 μs | 0.0110 μs | 1.062 μs |  0.82 | 0.0362 |     616 B |        1.24 |
