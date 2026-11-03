```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.77 μs | 12.885 μs | 0.706 μs |  1.00 |    0.02 |  1.0376 |  17.03 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  57.89 μs |  8.422 μs | 0.462 μs |  1.46 |    0.02 |  1.1597 |  19.59 KB |        1.15 |
| Invalidation_SequentialCommands | 416.64 μs | 10.600 μs | 0.581 μs | 10.48 |    0.16 | 10.2539 | 171.71 KB |       10.08 |
