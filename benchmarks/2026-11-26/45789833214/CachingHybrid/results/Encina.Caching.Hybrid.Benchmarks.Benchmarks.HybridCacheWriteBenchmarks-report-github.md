```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 3.862 μs | 0.3855 μs | 0.0211 μs |  0.92 |    0.03 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.554 μs | 6.1585 μs | 0.3376 μs |  2.28 |    0.10 | 0.1831 | 0.1678 |   3.56 KB |        3.43 |
| SetAsync                      | 4.191 μs | 2.9184 μs | 0.1600 μs |  1.00 |    0.05 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.560 μs | 1.6903 μs | 0.0927 μs |  1.09 |    0.04 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
