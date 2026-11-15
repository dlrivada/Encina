```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  24.46 μs | 1.223 μs | 0.067 μs |  1.00 |    0.00 | 0.1831 |  17.03 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  31.35 μs | 5.215 μs | 0.286 μs |  1.28 |    0.01 | 0.1831 |  19.43 KB |        1.14 |
| Invalidation_SequentialCommands | 249.87 μs | 9.897 μs | 0.543 μs | 10.22 |    0.03 | 1.9531 | 171.71 KB |       10.08 |
