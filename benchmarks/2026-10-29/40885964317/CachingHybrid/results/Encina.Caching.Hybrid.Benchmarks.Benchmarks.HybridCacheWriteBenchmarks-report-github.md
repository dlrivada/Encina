```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.844 μs | 5.0181 μs | 0.2751 μs |  1.08 |    0.06 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.033 μs | 3.8534 μs | 0.2112 μs |  2.25 |    0.08 | 0.1831 | 0.1678 |   3.57 KB |        3.43 |
| SetAsync                      |  4.470 μs | 2.6842 μs | 0.1471 μs |  1.00 |    0.04 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.386 μs | 0.3378 μs | 0.0185 μs |  0.98 |    0.03 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
