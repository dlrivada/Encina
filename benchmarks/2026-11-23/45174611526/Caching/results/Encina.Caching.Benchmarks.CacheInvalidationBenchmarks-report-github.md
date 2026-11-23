```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  40.11 μs |  2.348 μs | 0.129 μs |  1.00 |    0.00 |  1.0376 |  17.19 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  68.58 μs | 50.564 μs | 2.772 μs |  1.71 |    0.06 |  1.0986 |  19.49 KB |        1.13 |
| Invalidation_SequentialCommands | 424.44 μs |  8.849 μs | 0.485 μs | 10.58 |    0.03 | 10.2539 | 171.69 KB |        9.99 |
