```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 4.981 μs | 0.6928 μs | 0.0380 μs |  1.14 |    0.02 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 9.952 μs | 6.3415 μs | 0.3476 μs |  2.29 |    0.08 | 0.1678 | 0.1526 |   2.98 KB |        2.86 |
| SetAsync                      | 4.355 μs | 1.2605 μs | 0.0691 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 4.290 μs | 0.8564 μs | 0.0469 μs |  0.99 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
