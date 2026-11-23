```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  5.004 μs | 3.2815 μs | 0.1799 μs |  1.06 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.524 μs | 5.4973 μs | 0.3013 μs |  2.23 |    0.06 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  4.710 μs | 0.4671 μs | 0.0256 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.731 μs | 1.0491 μs | 0.0575 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
