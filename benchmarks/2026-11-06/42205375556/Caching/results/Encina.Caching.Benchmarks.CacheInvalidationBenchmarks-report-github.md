```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  27.33 μs |   1.127 μs | 0.062 μs |  1.00 |    0.00 | 0.1831 |  17.15 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  50.27 μs | 133.885 μs | 7.339 μs |  1.84 |    0.23 | 0.1831 |   19.8 KB |        1.15 |
| Invalidation_SequentialCommands | 295.02 μs | 103.037 μs | 5.648 μs | 10.80 |    0.18 | 1.9531 | 171.71 KB |       10.01 |
