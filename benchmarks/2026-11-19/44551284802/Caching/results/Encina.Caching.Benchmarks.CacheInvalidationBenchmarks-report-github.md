```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.85 μs |  0.091 μs | 0.005 μs |  1.00 |    0.00 |  1.0376 |  17.17 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  51.49 μs |  6.666 μs | 0.365 μs |  1.33 |    0.01 |  1.1597 |  19.52 KB |        1.14 |
| Invalidation_SequentialCommands | 424.19 μs | 36.888 μs | 2.022 μs | 10.92 |    0.05 | 10.2539 | 171.71 KB |       10.00 |
