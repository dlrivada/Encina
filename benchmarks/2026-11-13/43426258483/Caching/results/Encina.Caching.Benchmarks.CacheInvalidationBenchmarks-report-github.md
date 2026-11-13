```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  24.55 μs | 14.06 μs | 0.771 μs |  1.00 |    0.04 | 0.1831 |  17.33 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  49.09 μs | 20.30 μs | 1.113 μs |  2.00 |    0.07 | 0.1831 |  19.52 KB |        1.13 |
| Invalidation_SequentialCommands | 255.98 μs | 32.74 μs | 1.795 μs | 10.44 |    0.29 | 1.9531 | 171.72 KB |        9.91 |
