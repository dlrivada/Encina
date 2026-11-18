```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  20.09 μs | 10.42 μs | 0.571 μs |  1.00 |    0.03 |  1.0376 | 0.0305 |  17.29 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  28.65 μs | 17.33 μs | 0.950 μs |  1.43 |    0.05 |  1.1902 | 0.0305 |  19.69 KB |        1.14 |
| Invalidation_SequentialCommands | 204.08 μs | 24.93 μs | 1.366 μs | 10.16 |    0.25 | 10.4980 | 0.2441 | 171.71 KB |        9.93 |
