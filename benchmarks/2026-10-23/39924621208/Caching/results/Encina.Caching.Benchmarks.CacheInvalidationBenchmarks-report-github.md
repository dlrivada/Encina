```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.65 μs |  2.403 μs | 0.132 μs |  1.00 |    0.00 | 0.6714 |  17.45 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  59.50 μs | 51.115 μs | 2.802 μs |  1.54 |    0.06 | 0.7324 |  19.42 KB |        1.11 |
| Invalidation_SequentialCommands | 404.72 μs |  5.679 μs | 0.311 μs | 10.47 |    0.03 | 6.8359 | 171.71 KB |        9.84 |
