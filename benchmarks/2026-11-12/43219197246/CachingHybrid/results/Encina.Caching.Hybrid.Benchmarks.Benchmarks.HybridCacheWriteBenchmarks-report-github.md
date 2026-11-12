```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.016 μs | 2.319 μs | 0.1271 μs |  0.90 |    0.03 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.343 μs | 4.282 μs | 0.2347 μs |  2.32 |    0.07 | 0.1678 | 0.1526 |   2.97 KB |        2.86 |
| SetAsync                      |  4.467 μs | 2.261 μs | 0.1239 μs |  1.00 |    0.03 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.369 μs | 1.581 μs | 0.0867 μs |  0.98 |    0.03 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
