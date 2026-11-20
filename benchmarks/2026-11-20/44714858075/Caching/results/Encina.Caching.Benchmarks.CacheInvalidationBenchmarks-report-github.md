```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  38.36 μs |  1.079 μs | 0.059 μs |  1.00 |    0.00 |  1.0376 |  17.34 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  53.51 μs | 24.325 μs | 1.333 μs |  1.39 |    0.03 |  1.1597 |  19.52 KB |        1.13 |
| Invalidation_SequentialCommands | 419.51 μs | 21.452 μs | 1.176 μs | 10.94 |    0.03 | 10.2539 | 171.71 KB |        9.90 |
