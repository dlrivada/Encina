```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.72 μs |   0.946 μs | 0.052 μs |  1.00 |    0.00 |  1.0376 |   17.2 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  55.19 μs | 105.355 μs | 5.775 μs |  1.43 |    0.13 |  1.1597 |  19.65 KB |        1.14 |
| Invalidation_SequentialCommands | 409.19 μs |  62.444 μs | 3.423 μs | 10.57 |    0.08 | 10.2539 |  171.7 KB |        9.98 |
