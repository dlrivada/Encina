```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.826 μs | 7.5849 μs | 0.4158 μs |  1.02 |    0.08 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.282 μs | 0.7549 μs | 0.0414 μs |  2.18 |    0.03 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  4.711 μs | 1.1679 μs | 0.0640 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.464 μs | 0.3612 μs | 0.0198 μs |  0.95 |    0.01 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
