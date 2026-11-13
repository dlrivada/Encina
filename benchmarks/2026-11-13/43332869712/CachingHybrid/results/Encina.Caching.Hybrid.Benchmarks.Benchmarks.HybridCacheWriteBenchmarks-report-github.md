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
| RemoveAsync                   |  5.203 μs | 0.7863 μs | 0.0431 μs |  1.12 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.717 μs | 5.8528 μs | 0.3208 μs |  2.31 |    0.08 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  4.633 μs | 2.1191 μs | 0.1162 μs |  1.00 |    0.03 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.617 μs | 0.7135 μs | 0.0391 μs |  1.00 |    0.02 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
