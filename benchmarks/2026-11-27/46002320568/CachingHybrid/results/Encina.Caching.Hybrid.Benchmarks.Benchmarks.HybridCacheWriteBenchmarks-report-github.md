```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.483 μs | 2.340 μs | 0.1282 μs |  0.97 |    0.12 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.751 μs | 6.860 μs | 0.3760 μs |  1.88 |    0.23 | 0.0305 | 0.0153 |   2.98 KB |        2.86 |
| SetAsync                      | 3.635 μs | 8.903 μs | 0.4880 μs |  1.01 |    0.17 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.535 μs | 6.829 μs | 0.3743 μs |  0.98 |    0.15 | 0.0114 | 0.0076 |   1.05 KB |        1.01 |
