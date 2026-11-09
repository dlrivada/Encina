```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.409 μs | 0.0845 μs | 0.0046 μs |  0.86 |    0.01 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.419 μs | 0.0247 μs | 0.0014 μs |  0.86 |    0.01 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.645 μs | 0.4204 μs | 0.0230 μs |  1.00 |    0.02 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.338 μs | 0.2100 μs | 0.0115 μs |  0.81 |    0.01 | 0.0362 |     616 B |        1.24 |
