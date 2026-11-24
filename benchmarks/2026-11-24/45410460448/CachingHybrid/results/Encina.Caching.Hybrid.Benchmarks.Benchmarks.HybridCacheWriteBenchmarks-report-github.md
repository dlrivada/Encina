```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.738 μs | 1.353 μs | 0.0742 μs |  0.95 |    0.04 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 6.226 μs | 6.672 μs | 0.3657 μs |  2.15 |    0.14 | 0.0229 | 0.0153 |   2.39 KB |        2.30 |
| SetAsync                      | 2.902 μs | 2.410 μs | 0.1321 μs |  1.00 |    0.06 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 2.582 μs | 4.855 μs | 0.2661 μs |  0.89 |    0.09 | 0.0114 | 0.0076 |   1.05 KB |        1.01 |
