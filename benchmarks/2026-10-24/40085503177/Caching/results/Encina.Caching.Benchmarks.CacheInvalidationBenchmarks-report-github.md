```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  28.91 μs |   6.212 μs | 0.340 μs |  1.00 |    0.01 | 0.1831 |     17 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  45.55 μs |  63.831 μs | 3.499 μs |  1.58 |    0.11 | 0.1831 |  19.47 KB |        1.15 |
| Invalidation_SequentialCommands | 307.03 μs | 150.387 μs | 8.243 μs | 10.62 |    0.27 | 1.9531 | 171.72 KB |       10.10 |
