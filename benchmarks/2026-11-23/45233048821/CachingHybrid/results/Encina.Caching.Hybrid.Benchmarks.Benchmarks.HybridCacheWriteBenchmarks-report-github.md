```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.820 μs | 0.5080 μs | 0.0278 μs |  0.85 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.408 μs | 3.2600 μs | 0.1787 μs |  1.87 |    0.04 | 0.1678 | 0.1526 |   2.96 KB |        2.85 |
| SetAsync                      | 4.487 μs | 1.2633 μs | 0.0692 μs |  1.00 |    0.02 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.450 μs | 2.6680 μs | 0.1462 μs |  0.99 |    0.03 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
