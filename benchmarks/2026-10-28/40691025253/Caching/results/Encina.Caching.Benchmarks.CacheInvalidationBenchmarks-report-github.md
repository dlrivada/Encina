```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  34.00 μs |  2.974 μs | 0.163 μs |  1.00 |    0.01 |  1.0376 |  17.31 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  49.97 μs | 46.828 μs | 2.567 μs |  1.47 |    0.07 |  1.1597 |  19.71 KB |        1.14 |
| Invalidation_SequentialCommands | 368.12 μs |  2.629 μs | 0.144 μs | 10.83 |    0.05 | 10.2539 | 171.71 KB |        9.92 |
