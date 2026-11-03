```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.970 μs | 0.0668 μs | 0.0037 μs |  1.09 |    0.00 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.213 μs | 1.1403 μs | 0.0625 μs |  2.24 |    0.02 | 0.1678 | 0.1526 |   2.98 KB |        2.86 |
| SetAsync                      |  4.551 μs | 0.4114 μs | 0.0225 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.447 μs | 1.1817 μs | 0.0648 μs |  0.98 |    0.01 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
