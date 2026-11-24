```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.818 μs | 1.547 μs | 0.0848 μs |  0.97 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.772 μs | 1.383 μs | 0.0758 μs |  2.18 |    0.06 | 0.1678 | 0.1526 |   2.98 KB |        2.86 |
| SetAsync                      |  4.947 μs | 3.035 μs | 0.1663 μs |  1.00 |    0.04 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.837 μs | 2.331 μs | 0.1278 μs |  0.98 |    0.04 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
