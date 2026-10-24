```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  32.41 μs |  13.52 μs | 0.741 μs |  1.00 |    0.03 | 0.1831 |   17.3 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  50.79 μs | 128.55 μs | 7.046 μs |  1.57 |    0.19 | 0.1831 |   19.5 KB |        1.13 |
| Invalidation_SequentialCommands | 343.34 μs |  46.60 μs | 2.554 μs | 10.60 |    0.22 | 1.9531 | 171.69 KB |        9.92 |
