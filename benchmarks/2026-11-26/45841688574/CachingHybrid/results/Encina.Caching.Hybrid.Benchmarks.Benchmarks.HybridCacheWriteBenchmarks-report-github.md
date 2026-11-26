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
| RemoveAsync                   | 3.843 μs | 0.1363 μs | 0.0075 μs |  0.93 |    0.01 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 8.302 μs | 0.5147 μs | 0.0282 μs |  2.01 |    0.02 | 0.1831 | 0.1678 |   3.56 KB |        3.43 |
| SetAsync                      | 4.133 μs | 0.6442 μs | 0.0353 μs |  1.00 |    0.01 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.098 μs | 0.8383 μs | 0.0460 μs |  0.99 |    0.01 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
