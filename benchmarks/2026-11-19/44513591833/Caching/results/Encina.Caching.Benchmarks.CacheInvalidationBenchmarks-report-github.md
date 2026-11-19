```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  33.94 μs |  1.538 μs | 0.084 μs |  1.00 |    0.00 |  1.0376 |  17.27 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  49.33 μs | 45.027 μs | 2.468 μs |  1.45 |    0.06 |  1.1597 |  19.88 KB |        1.15 |
| Invalidation_SequentialCommands | 356.62 μs | 23.762 μs | 1.302 μs | 10.51 |    0.04 | 10.2539 | 171.73 KB |        9.95 |
