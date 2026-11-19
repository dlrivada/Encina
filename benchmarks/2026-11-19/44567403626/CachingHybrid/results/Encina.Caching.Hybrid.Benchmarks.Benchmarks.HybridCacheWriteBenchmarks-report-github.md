```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.706 μs | 0.5430 μs | 0.0298 μs |  0.87 |    0.03 | 0.0114 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 5.650 μs | 5.6202 μs | 0.3081 μs |  1.82 |    0.11 | 0.0153 |      - |   2.39 KB |        2.30 |
| SetAsync                      | 3.107 μs | 2.3850 μs | 0.1307 μs |  1.00 |    0.05 | 0.0114 | 0.0076 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 2.999 μs | 2.7039 μs | 0.1482 μs |  0.97 |    0.05 | 0.0114 | 0.0076 |   1.05 KB |        1.01 |
