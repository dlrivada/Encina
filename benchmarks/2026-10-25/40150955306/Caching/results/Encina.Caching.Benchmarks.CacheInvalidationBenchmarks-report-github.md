```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|----------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.63 μs | 0.582 μs | 0.797 μs |  39.16 μs |  1.00 |    0.03 |  1.0376 |  17.34 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  67.29 μs | 5.114 μs | 7.497 μs |  66.55 μs |  1.70 |    0.19 |  1.0986 |  19.62 KB |        1.13 |
| Invalidation_SequentialCommands | 417.78 μs | 2.372 μs | 3.401 μs | 417.36 μs | 10.55 |    0.22 | 10.2539 | 171.71 KB |        9.90 |
