```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  32.54 μs |   4.759 μs | 0.261 μs |  1.00 |    0.01 | 0.1831 |  17.22 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  54.15 μs | 126.370 μs | 6.927 μs |  1.66 |    0.18 | 0.1831 |  19.51 KB |        1.13 |
| Invalidation_SequentialCommands | 339.09 μs |   3.675 μs | 0.201 μs | 10.42 |    0.07 | 1.9531 | 171.69 KB |        9.97 |
