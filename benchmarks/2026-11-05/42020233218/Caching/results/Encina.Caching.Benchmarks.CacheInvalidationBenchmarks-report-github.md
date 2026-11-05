```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.16 μs |  2.296 μs | 0.126 μs |  1.00 |    0.00 |  1.0376 |  17.13 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  54.01 μs | 60.469 μs | 3.315 μs |  1.38 |    0.07 |  1.1597 |  19.23 KB |        1.12 |
| Invalidation_SequentialCommands | 404.48 μs | 23.130 μs | 1.268 μs | 10.33 |    0.04 | 10.2539 |  171.7 KB |       10.02 |
