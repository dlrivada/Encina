```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.65 μs |   2.374 μs | 0.130 μs |  1.00 |    0.00 | 0.6714 |  17.18 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  58.71 μs | 143.732 μs | 7.878 μs |  1.48 |    0.17 | 0.7324 |  19.41 KB |        1.13 |
| Invalidation_SequentialCommands | 406.75 μs |  19.770 μs | 1.084 μs | 10.26 |    0.04 | 6.8359 | 171.72 KB |        9.99 |
