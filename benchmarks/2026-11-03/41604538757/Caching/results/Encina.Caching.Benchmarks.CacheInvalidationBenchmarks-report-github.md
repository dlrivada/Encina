```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  37.91 μs |  1.172 μs | 0.064 μs |  1.00 |    0.00 |  1.0376 |  17.32 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  63.91 μs | 44.929 μs | 2.463 μs |  1.69 |    0.06 |  1.0986 |   19.4 KB |        1.12 |
| Invalidation_SequentialCommands | 425.66 μs |  1.124 μs | 0.062 μs | 11.23 |    0.02 | 10.2539 | 171.71 KB |        9.91 |
