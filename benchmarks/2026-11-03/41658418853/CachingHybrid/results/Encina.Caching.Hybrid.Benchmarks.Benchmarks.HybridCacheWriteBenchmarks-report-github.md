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
| RemoveAsync                   |  4.947 μs | 0.3893 μs | 0.0213 μs |  1.11 |    0.02 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.071 μs | 3.4644 μs | 0.1899 μs |  2.26 |    0.05 | 0.1678 | 0.1526 |   2.98 KB |        2.87 |
| SetAsync                      |  4.452 μs | 1.6145 μs | 0.0885 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.470 μs | 0.3585 μs | 0.0196 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
