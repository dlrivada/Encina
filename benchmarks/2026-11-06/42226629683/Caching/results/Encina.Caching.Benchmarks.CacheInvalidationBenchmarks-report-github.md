```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.77 μs |  0.679 μs | 0.037 μs |  1.00 |    0.00 | 0.6714 |  17.18 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  55.86 μs | 18.829 μs | 1.032 μs |  1.40 |    0.02 | 0.7935 |  19.62 KB |        1.14 |
| Invalidation_SequentialCommands | 411.61 μs | 10.481 μs | 0.574 μs | 10.35 |    0.02 | 6.8359 | 171.68 KB |        9.99 |
