```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.27 μs |   0.997 μs |  0.055 μs |  1.00 |    0.00 |  1.0376 |  17.49 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  72.47 μs | 202.668 μs | 11.109 μs |  1.89 |    0.25 |  1.0986 |  19.48 KB |        1.11 |
| Invalidation_SequentialCommands | 418.15 μs |   8.728 μs |  0.478 μs | 10.92 |    0.02 | 10.2539 | 171.72 KB |        9.82 |
