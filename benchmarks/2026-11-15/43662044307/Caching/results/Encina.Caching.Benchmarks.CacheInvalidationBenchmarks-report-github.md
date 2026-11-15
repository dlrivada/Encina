```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  24.51 μs | 0.665 μs | 0.975 μs |  1.00 |    0.05 | 0.1831 |   17.3 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  39.34 μs | 1.986 μs | 2.911 μs |  1.61 |    0.13 | 0.1831 |  19.56 KB |        1.13 |
| Invalidation_SequentialCommands | 254.72 μs | 6.553 μs | 9.186 μs | 10.41 |    0.54 | 1.9531 | 171.68 KB |        9.92 |
