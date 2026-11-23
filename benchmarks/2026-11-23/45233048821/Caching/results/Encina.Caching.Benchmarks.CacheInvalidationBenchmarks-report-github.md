```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  25.01 μs | 0.497 μs | 0.027 μs |  1.00 |    0.00 | 0.1831 |  17.17 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  32.48 μs | 6.998 μs | 0.384 μs |  1.30 |    0.01 | 0.1831 |  19.58 KB |        1.14 |
| Invalidation_SequentialCommands | 259.33 μs | 8.136 μs | 0.446 μs | 10.37 |    0.02 | 1.9531 | 171.71 KB |       10.00 |
