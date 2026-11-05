```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.898 μs | 0.1656 μs | 0.0091 μs |  1.07 |    0.00 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.095 μs | 4.8233 μs | 0.2644 μs |  2.21 |    0.05 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      |  4.578 μs | 0.2194 μs | 0.0120 μs |  1.00 |    0.00 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.565 μs | 0.5057 μs | 0.0277 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
