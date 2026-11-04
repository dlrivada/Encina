```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Invalidation_NoMatchingKeys     |  33.02 μs | 12.13 μs | 0.665 μs |  1.00 |    0.02 | 0.1831 |  17.03 KB |        1.00 |
| Invalidation_WithMatchingKeys   |  71.88 μs | 30.71 μs | 1.683 μs |  2.18 |    0.06 | 0.1221 |  19.75 KB |        1.16 |
| Invalidation_SequentialCommands | 343.05 μs | 14.97 μs | 0.821 μs | 10.39 |    0.18 | 1.9531 | 171.71 KB |       10.08 |
