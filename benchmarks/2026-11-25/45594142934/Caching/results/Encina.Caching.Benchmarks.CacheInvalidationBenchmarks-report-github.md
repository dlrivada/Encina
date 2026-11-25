```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  30.60 μs |  3.557 μs | 0.195 μs |  1.00 |    0.01 |  1.0376 |  17.21 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  49.40 μs | 22.764 μs | 1.248 μs |  1.61 |    0.04 |  1.1597 |  19.49 KB |        1.13 |
| Invalidation_SequentialCommands | 323.16 μs | 95.834 μs | 5.253 μs | 10.56 |    0.16 | 10.2539 |  171.7 KB |        9.98 |
