```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  26.36 μs |  3.679 μs | 0.202 μs |  1.00 |    0.01 |  1.0376 | 0.0305 |   17.3 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  41.63 μs | 29.903 μs | 1.639 μs |  1.58 |    0.05 |  1.1597 |      - |  19.57 KB |        1.13 |
| Invalidation_SequentialCommands | 277.21 μs | 14.268 μs | 0.782 μs | 10.52 |    0.07 | 10.2539 |      - | 171.71 KB |        9.93 |
