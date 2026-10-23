```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  40.10 μs |  3.134 μs | 0.172 μs |  1.00 |    0.01 |  1.0376 |  17.34 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  66.77 μs | 90.303 μs | 4.950 μs |  1.67 |    0.11 |  1.1597 |  19.37 KB |        1.12 |
| Invalidation_SequentialCommands | 419.43 μs | 31.891 μs | 1.748 μs | 10.46 |    0.05 | 10.2539 | 171.69 KB |        9.90 |
