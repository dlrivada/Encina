```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  19.56 μs |   0.972 μs | 0.053 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |  17.24 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  29.37 μs |  33.086 μs | 1.814 μs |  1.50 |    0.08 |  1.1902 | 0.2136 |   19.5 KB |        1.13 |
| Invalidation_SequentialCommands | 206.45 μs | 139.805 μs | 7.663 μs | 10.55 |    0.34 | 10.4980 | 0.2441 |  171.7 KB |        9.96 |
