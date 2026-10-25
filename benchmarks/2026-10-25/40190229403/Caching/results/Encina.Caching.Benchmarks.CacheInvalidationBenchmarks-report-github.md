```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.69 μs |  2.088 μs | 0.114 μs |  1.00 |    0.00 |  1.0376 |   17.2 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  60.57 μs | 97.629 μs | 5.351 μs |  1.57 |    0.12 |  1.0986 |  19.49 KB |        1.13 |
| Invalidation_SequentialCommands | 415.96 μs |  7.795 μs | 0.427 μs | 10.75 |    0.03 | 10.2539 | 171.71 KB |        9.99 |
