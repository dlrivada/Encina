```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.624 μs | 1.814 μs | 0.0994 μs |  1.01 |    0.09 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.168 μs | 8.533 μs | 0.4677 μs |  1.99 |    0.21 | 0.0305 | 0.0153 |   2.98 KB |        2.87 |
| SetAsync                      | 3.629 μs | 6.518 μs | 0.3573 μs |  1.01 |    0.12 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.809 μs | 2.661 μs | 0.1459 μs |  1.06 |    0.10 | 0.0114 | 0.0076 |   1.08 KB |        1.04 |
