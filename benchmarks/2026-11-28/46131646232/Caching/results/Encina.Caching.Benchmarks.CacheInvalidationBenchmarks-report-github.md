```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.81 μs |  1.712 μs | 0.094 μs |  1.00 |    0.00 |  1.0376 |  17.21 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  50.55 μs |  3.108 μs | 0.170 μs |  1.27 |    0.00 |  1.1597 |  19.63 KB |        1.14 |
| Invalidation_SequentialCommands | 413.93 μs | 13.976 μs | 0.766 μs | 10.40 |    0.03 | 10.2539 | 171.72 KB |        9.98 |
