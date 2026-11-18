```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.962 μs | 3.7624 μs | 0.2062 μs |  1.01 |    0.04 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 11.301 μs | 3.6054 μs | 0.1976 μs |  2.30 |    0.04 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  4.917 μs | 0.7981 μs | 0.0437 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.815 μs | 2.4001 μs | 0.1316 μs |  0.98 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
