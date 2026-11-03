```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.739 μs | 4.1574 μs | 0.2279 μs |  1.07 |    0.05 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.639 μs | 1.3002 μs | 0.0713 μs |  2.41 |    0.03 | 0.1831 | 0.1678 |   3.55 KB |        3.42 |
| SetAsync                      |  4.423 μs | 0.9986 μs | 0.0547 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.520 μs | 1.5216 μs | 0.0834 μs |  1.02 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
