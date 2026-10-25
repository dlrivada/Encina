```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  5.102 μs | 0.2861 μs | 0.0157 μs |  1.17 |    0.00 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.364 μs | 5.7557 μs | 0.3155 μs |  2.38 |    0.06 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      |  4.357 μs | 0.2418 μs | 0.0133 μs |  1.00 |    0.00 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.447 μs | 0.9432 μs | 0.0517 μs |  1.02 |    0.01 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
