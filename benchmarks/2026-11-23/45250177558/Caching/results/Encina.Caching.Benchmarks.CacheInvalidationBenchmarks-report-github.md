```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  18.65 μs |  17.537 μs | 0.961 μs |  1.00 |    0.06 |  1.0376 | 0.0305 |  17.16 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  27.69 μs |   8.778 μs | 0.481 μs |  1.49 |    0.07 |  1.1902 | 0.0305 |  19.46 KB |        1.13 |
| Invalidation_SequentialCommands | 198.23 μs | 154.730 μs | 8.481 μs | 10.65 |    0.61 | 10.4980 | 0.2441 | 171.71 KB |       10.01 |
