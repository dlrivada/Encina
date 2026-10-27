```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 5.048 μs | 0.2054 μs | 0.0113 μs |  1.15 |    0.01 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.694 μs | 1.6205 μs | 0.0888 μs |  2.21 |    0.02 | 0.1831 | 0.1678 |   3.56 KB |        3.43 |
| SetAsync                      | 4.389 μs | 0.5419 μs | 0.0297 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.364 μs | 1.2484 μs | 0.0684 μs |  0.99 |    0.01 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
