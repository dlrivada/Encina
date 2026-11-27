```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.18 μs |  6.799 μs | 0.373 μs |  1.00 |    0.01 |  1.0376 |  17.12 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  56.28 μs | 47.171 μs | 2.586 μs |  1.44 |    0.06 |  1.1597 |  19.25 KB |        1.12 |
| Invalidation_SequentialCommands | 426.48 μs |  1.901 μs | 0.104 μs | 10.89 |    0.09 | 10.2539 |  171.7 KB |       10.03 |
