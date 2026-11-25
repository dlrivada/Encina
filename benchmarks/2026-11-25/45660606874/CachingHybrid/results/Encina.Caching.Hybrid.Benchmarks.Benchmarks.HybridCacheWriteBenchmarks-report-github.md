```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.935 μs | 0.2132 μs | 0.0117 μs |  0.97 |    0.02 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.723 μs | 1.5738 μs | 0.0863 μs |  2.11 |    0.04 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  5.084 μs | 1.8008 μs | 0.0987 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.919 μs | 1.4070 μs | 0.0771 μs |  0.97 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
