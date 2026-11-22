```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|----------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  40.04 μs | 0.624 μs | 0.875 μs |  39.52 μs |  1.00 |    0.03 |  1.0376 |  17.06 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  56.04 μs | 2.240 μs | 3.283 μs |  56.10 μs |  1.40 |    0.09 |  1.1597 |  19.24 KB |        1.13 |
| Invalidation_SequentialCommands | 424.92 μs | 1.282 μs | 1.839 μs | 425.38 μs | 10.62 |    0.23 | 10.2539 |  171.7 KB |       10.06 |
