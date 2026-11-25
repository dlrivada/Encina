```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.695 μs | 0.2388 μs | 0.0131 μs |  0.77 |    0.02 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.749 μs | 3.3570 μs | 0.1840 μs |  2.22 |    0.07 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.490 μs | 1.8013 μs | 0.0987 μs |  1.00 |    0.03 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.566 μs | 0.8493 μs | 0.0466 μs |  1.02 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
