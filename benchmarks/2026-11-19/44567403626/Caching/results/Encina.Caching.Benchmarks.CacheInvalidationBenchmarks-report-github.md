```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  24.63 μs |  0.334 μs | 0.018 μs |  1.00 |    0.00 | 0.2136 |      - |  17.45 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  34.05 μs |  2.534 μs | 0.139 μs |  1.38 |    0.00 | 0.1831 | 0.0610 |  19.48 KB |        1.12 |
| Invalidation_SequentialCommands | 260.15 μs | 34.599 μs | 1.897 μs | 10.56 |    0.07 | 1.9531 |      - | 171.71 KB |        9.84 |
