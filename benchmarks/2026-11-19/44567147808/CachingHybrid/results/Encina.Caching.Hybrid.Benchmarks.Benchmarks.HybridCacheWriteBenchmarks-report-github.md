```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.889 μs | 7.5800 μs | 0.4155 μs |  1.04 |    0.08 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.480 μs | 3.8496 μs | 0.2110 μs |  2.23 |    0.05 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      |  4.709 μs | 1.6271 μs | 0.0892 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.592 μs | 0.6475 μs | 0.0355 μs |  0.98 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
