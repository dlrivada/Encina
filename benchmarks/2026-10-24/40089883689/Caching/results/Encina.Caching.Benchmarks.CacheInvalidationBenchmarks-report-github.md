```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  39.15 μs |  0.789 μs | 0.043 μs |  1.00 |    0.00 |  1.0376 |  17.25 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  70.08 μs | 85.748 μs | 4.700 μs |  1.79 |    0.10 |  1.1597 |  19.46 KB |        1.13 |
| Invalidation_SequentialCommands | 413.55 μs | 21.859 μs | 1.198 μs | 10.56 |    0.03 | 10.2539 |  171.7 KB |        9.95 |
