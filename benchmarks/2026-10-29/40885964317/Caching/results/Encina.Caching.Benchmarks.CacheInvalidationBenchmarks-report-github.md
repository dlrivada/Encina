```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  18.03 μs |  0.375 μs | 0.021 μs |  1.00 |    0.00 |  1.0376 | 0.0305 |   17.3 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  27.81 μs | 45.073 μs | 2.471 μs |  1.54 |    0.12 |  1.1597 |      - |  19.37 KB |        1.12 |
| Invalidation_SequentialCommands | 192.50 μs | 26.853 μs | 1.472 μs | 10.67 |    0.07 | 10.4980 | 0.2441 | 171.71 KB |        9.93 |
