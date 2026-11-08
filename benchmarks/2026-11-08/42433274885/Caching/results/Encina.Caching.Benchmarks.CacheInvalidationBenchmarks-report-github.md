```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  19.44 μs | 0.307 μs | 0.460 μs |  1.00 |    0.03 |  1.0376 | 0.0305 |  17.21 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  28.29 μs | 0.948 μs | 1.389 μs |  1.46 |    0.08 |  1.1902 | 0.0305 |  19.62 KB |        1.14 |
| Invalidation_SequentialCommands | 202.23 μs | 1.429 μs | 2.004 μs | 10.41 |    0.27 | 10.4980 | 0.2441 | 171.78 KB |        9.98 |
